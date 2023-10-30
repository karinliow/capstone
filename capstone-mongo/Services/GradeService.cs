using MongoDB.Driver;
using capstone_mongo.Models;
using capstone_mongo.Helper;
using static capstone_mongo.Models.Grade;
using System.Collections;

namespace capstone_mongo.Services
{
    public class GradeService
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<Grade> gradesCollection;
        private readonly IMongoCollection<Team> teams;

        private readonly SessionService sessionService;

        private readonly ModuleService moduleService;
        private readonly StudentService studentService;
        private readonly TeamService teamService;
        private readonly PeerEvalService evalService;

        //private readonly string moduleCode;

        public GradeService(IServiceProvider sp,
                            SessionService sessionService,
                            ModuleService moduleService,
                            StudentService studentService,
                            TeamService teamService,
                            PeerEvalService evalService)
        {
            this.sessionService = sessionService;

            this.moduleService = moduleService;
            this.studentService = studentService;
            this.teamService = teamService;
            this.evalService = evalService;

            if (!MongoConfig.IsUserLoggedIn())
            {
                // Redirect to "No Access" page or perform any other action
                throw new AccessDenied("Access denied. User is not logged in.");
            }

            database = MongoConfig.GetDatabase(sp);
            gradesCollection = MongoConfig.GetMongoCollection<Grade>(database, sessionService.ModuleCode);
        }
        public Grade Get(string studentId)
        {
            return gradesCollection.Find(s => s.Id == studentId).FirstOrDefault();
        }

        public List<Grade> GetAllGrades()
        {
            return gradesCollection.Find(g => true).ToList();
        }

        public async Task<IEnumerable<Grade>> CalculateGradesAsync()
        {
            // retrieve all grades
            var grades = await gradesCollection.AsQueryable().ToListAsync();
            Module modCode = await moduleService.GetModuleAsync(sessionService.ModuleCode);

            foreach (var grade in grades)
            {
                var currSid = grade.Id;
                var group = await teamService.GetGroupAsync(currSid);

                var finalScore = 0.0;

                if (modCode != null)
                {
                    foreach (var assessment in grade.AssessmentScores)
                    {
                        var score = 0.0;

                        var assessmentName = assessment.Key;
                        var assessmentScore = assessment.Value;

                        var getAssessment = modCode.Assessments.FirstOrDefault(m => m.AssessmentName == assessmentName);
                        var weight = getAssessment.Weightage / 100.0;

                        if (getAssessment.PeerEvaluation && group != null)
                        {
                            var getPeerWeightage = getAssessment.PeerWeightage;
                            var peerScore = await evalService.CalculatePeerEvaluationAsync(grade.Id, assessmentName, group);
                            peerScore = (peerScore / 10) * (getPeerWeightage / 100.0);

                            // remaining percentage
                            var remaining = (100 - getPeerWeightage) / 100.0;
                            var rawScore = (assessmentScore / getAssessment.MaxScore) * remaining;

                            score = (rawScore + peerScore) * weight;
                        }
                        else
                        {
                            // if grade has no peer evaluation
                            var rawScore = (assessmentScore / getAssessment.MaxScore) * weight;

                            score = (assessmentScore / getAssessment.MaxScore) * weight;
                        }

                        finalScore += score;
                    }
                    finalScore = Math.Round(finalScore, 4);
                    grade.FinalScore = Math.Round(finalScore * 100, 2);

                    // Update the FinalScore in MongoDB for the current grade
                    var filter = Builders<Grade>.Filter.Eq("_id", grade.Id);
                    var update = Builders<Grade>.Update.Set(x => x.FinalScore, grade.FinalScore);

                    await gradesCollection.UpdateOneAsync(filter, update);
                }
            }
            return grades;
        }


        public Grade Add(Grade grade)
        {
            gradesCollection.InsertOne(grade);
            return grade;
        }

        public async Task BulkInsertOrUpdate(IEnumerable<Grade> gradesList)
        {
            var bulkUpdateList = new List<WriteModel<Grade>>();
            var updateBuilder = Builders<Grade>.Update;

            foreach (var g in gradesList)
            {
                var filter = Builders<Grade>.Filter.Eq(s => s.Id, g.Id);

                var update = updateBuilder
                    .Set(s => s.AssessmentScores, g.AssessmentScores);

                var updateModel = new UpdateOneModel<Grade>(filter, update)
                {
                    IsUpsert = true
                };

                bulkUpdateList.Add(updateModel);
            }

            if (bulkUpdateList.Count > 0)
                await gradesCollection.BulkWriteAsync(bulkUpdateList);
        }

        public async Task PopulateAssessmentsAsync(string moduleCode)
        {
            Module module = await moduleService.GetModuleAsync(moduleCode);
            List<Student> students = await studentService.GetStudentsByModuleAsync(moduleCode);

            var grades = students.Select(student => new Grade
            {
                Id = student.StudentId,
                StudentName = student.Name,
                ModuleCode = moduleCode,
                AssessmentScores = module.Assessments.ToDictionary(a => a.AssessmentName, _ => 0.0),
                FinalScore = 0
            }).ToList();

            await SaveGradeAsync(grades);
        }

        public async Task SaveGradeAsync(List<Grade> grades)
        {
            if (grades.Count > 0)
            {
                var existingStudentIds = grades.Select(g => g.Id);
                var filter = Builders<Grade>.Filter.In("_id", existingStudentIds);
                var existingGrades = await gradesCollection.Find(filter).ToListAsync();

                var newGrades = grades.Where(g => !existingGrades.Any(eg => eg.Id == g.Id)).ToList();

                if (newGrades.Count > 0)
                {

                    await gradesCollection.InsertManyAsync(newGrades);
                }
            }
        }

        public async Task<Grade> UpdateAsync(Grade grade)
        {
            var existingGrade = await gradesCollection.FindOneAndUpdateAsync(
                Builders<Grade>.Filter.Eq(g => g.Id, grade.Id),
                Builders<Grade>.Update
                .Set(g => g.AssessmentScores, grade.AssessmentScores)
                .Set(g => g.FinalScore, grade.FinalScore),
                new FindOneAndUpdateOptions<Grade>
                {
                    ReturnDocument = ReturnDocument.After
                });

            return existingGrade;
        }


        // CHART SECTION:
        public double CalculateAverage()
        {
            var grades = GetAllGrades();
            var finalScores = grades.Select(g => g.FinalScore);
            var mean = finalScores.Average();

            return mean;
        }


        public double CalculateMedian()
        {
            var grades = GetAllGrades();

            if (grades.Count == 0)
                return 0;

            var sortedGrades = grades.OrderBy(g => g.FinalScore).ToList();

            if (grades.Count % 2 == 0)
            {
                var middleIndex = grades.Count / 2;
                var median = (sortedGrades[middleIndex - 1].FinalScore + sortedGrades[middleIndex].FinalScore) / 2;
                return median;
            }
            else
            {
                var middleIndex = grades.Count / 2;
                var median = sortedGrades[middleIndex].FinalScore;
                return median;
            }
        }

        public List<double> CalculateMode()
        {
            var grades = GetAllGrades();

            if (grades.Count == 0)
                return new List<double>();

            var scoreCounts = new Dictionary<double, int>();

            foreach (var grade in grades)
            {
                if (scoreCounts.ContainsKey(grade.FinalScore))
                    scoreCounts[grade.FinalScore]++;
                else
                    scoreCounts[grade.FinalScore] = 1;
            }

            var maxCount = scoreCounts.Values.Max();
            var modes = scoreCounts.Where(x => x.Value == maxCount).Select(x => x.Key).ToList();

            return modes;
        }

        public double GetMin()
        {
            var grades = GetAllGrades();

            if (grades.Count == 0)
                return 0;

            var min = grades.Min(g => g.FinalScore);
            return min;
        }

        public double GetMax()
        {
            var grades = GetAllGrades();

            if (grades.Count == 0)
                return 0;

            var max = grades.Max(g => g.FinalScore);
            return max;
        }
        public List<double> CalcMeanAssignment()
        {
            var grades = GetAllGrades();
            var assignments = grades.SelectMany(g => g.AssessmentScores.Keys).Distinct();
            var means = new List<double>();

            foreach (var assignment in assignments)
            {
                var scores = grades.Where(g => g.AssessmentScores.ContainsKey(assignment))
                                   .Select(g => g.AssessmentScores[assignment]);
                var mean = scores.Average();
                means.Add(mean);
            }

            return means;
        }

        public List<double> CalcStdDevAssignment()
        {
            var grades = GetAllGrades();
            var assignments = grades.SelectMany(g => g.AssessmentScores.Keys).Distinct();
            var stdDeviations = new List<double>();

            foreach (var assignment in assignments)
            {
                var scores = grades.Where(g => g.AssessmentScores.ContainsKey(assignment))
                                   .Select(g => g.AssessmentScores[assignment]);
                var stdDeviation = CalcStdDev(scores);
                stdDeviations.Add(stdDeviation);
            }

            return stdDeviations;
        }

        private double CalcStdDev(IEnumerable<double> scores)
        {
            var count = scores.Count();
            var mean = scores.Average();
            var sumSquaredDifferences = scores.Sum(score => Math.Pow(score - mean, 2));
            var variance = sumSquaredDifferences / count;
            var stdDeviation = Math.Sqrt(variance);
            return stdDeviation;
        }

        public List<string> GetAssignment()
        {
            var module = moduleService.GetModuleAsync(sessionService.ModuleCode).Result;

            if (module == null || module.Assessments == null || module.Assessments.Count == 0)
                return new List<string>();

            return module.Assessments.Select(a => a.AssessmentName).ToList();
        }

        public List<double> GetMaxAssignment()
        {
            var grades = GetAllGrades();
            var assignments = GetAssignment();
            var maxValues = new List<double>();

            foreach (var assignment in assignments)
            {
                var assignmentGrades = grades.Where(g => g.AssessmentScores.ContainsKey(assignment))
                                            .Select(g => g.AssessmentScores[assignment]);
                var max = assignmentGrades.Max();
                maxValues.Add(max);
            }

            return maxValues;
        }

        public List<double> GetMinAssignment()
        {
            var grades = GetAllGrades();
            var assignments = GetAssignment();
            var minValues = new List<double>();

            foreach (var assignment in assignments)
            {
                var assignmentGrades = grades.Where(g => g.AssessmentScores.ContainsKey(assignment))
                                            .Select(g => g.AssessmentScores[assignment]);
                var min = assignmentGrades.Min();
                minValues.Add(min);
            }

            return minValues;
        }

        public List<double> GetModeAssignment()
        {
            var grades = GetAllGrades();
            var assignments = GetAssignment();
            var modeValues = new List<double>();

            foreach (var assignment in assignments)
            {
                var assignmentGrades = grades.Where(g => g.AssessmentScores.ContainsKey(assignment))
                                            .Select(g => g.AssessmentScores[assignment]);

                var scoreCounts = new Dictionary<double, int>();

                foreach (var grade in assignmentGrades)
                {
                    if (scoreCounts.ContainsKey(grade))
                        scoreCounts[grade]++;
                    else
                        scoreCounts[grade] = 1;
                }

                var maxCount = scoreCounts.Values.Max();
                var modes = scoreCounts.Where(x => x.Value == maxCount).Select(x => x.Key).ToList();

                var mode = modes.FirstOrDefault(); // Assign the mode value or default to 0 if modes list is empty
                modeValues.Add(mode);
            }

            return modeValues;
        }

        public List<double> GetMedianAssignment()
        {
            var grades = GetAllGrades();
            var assignments = GetAssignment();
            var medianValues = new List<double>();

            foreach (var assignment in assignments)
            {
                var assignmentGrades = grades.Where(g => g.AssessmentScores.ContainsKey(assignment))
                                             .Select(g => g.AssessmentScores[assignment])
                                             .ToList();

                var sortedGrades = assignmentGrades.OrderBy(g => g).ToList();
                var count = sortedGrades.Count;

                if (count == 0)
                {
                    medianValues.Add(0);
                    continue;
                }

                var middleIndex = count / 2;
                double median;

                if (count % 2 == 0)
                {
                    var medianSum = sortedGrades[middleIndex - 1] + sortedGrades[middleIndex];
                    median = medianSum / 2;
                }
                else
                {
                    median = sortedGrades[middleIndex];
                }

                medianValues.Add(median);
            }

            return medianValues;
        }

        public List<double> GetOverallGrades()
        {
            var grades = GetAllGrades();
            var overallGrades = grades.Select(g => g.FinalScore).ToList();
            return overallGrades;
        }

        public List<double> GetBellCurveData()
        {
            var overallGrades = GetOverallGrades();

            // Calculate the mean and standard deviation of the overall grades
            var mean = overallGrades.Average();
            var standardDeviation = CalculateStandardDeviation(overallGrades);

            // Calculate the bell curve y-values based on the overall grades
            var bellCurveData = overallGrades.Select(grade => CalculateBellCurveValue(grade, mean, standardDeviation)).ToList();

            return bellCurveData;
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var count = values.Count();
            var mean = values.Average();
            var sumSquaredDifferences = values.Sum(value => Math.Pow(value - mean, 2));
            var variance = sumSquaredDifferences / count;
            var standardDeviation = Math.Sqrt(variance);
            return standardDeviation;
        }

        private double CalculateBellCurveValue(double x, double mean, double standardDeviation)
        {
            var exponent = -((x - mean) * (x - mean)) / (2 * standardDeviation * standardDeviation);
            var coefficient = 1 / (standardDeviation * Math.Sqrt(2 * Math.PI));
            var bellCurveValue = coefficient * Math.Exp(exponent);
            return bellCurveValue;
        }
    }

}

