using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace capstone_mongo.Models
{
    public class Grade
    {
        [BsonId]
        [BsonElement("_id")] 
        [Display(Name = "Student ID (SIT)")]
        public string Id { get; set; }

        [BsonElement("StudentName")] 
        [Required]
        [Display(Name = "Name")]
        public string StudentName { get; set; }

        [BsonElement("ModuleCode")]
        [Required]
        [Display(Name = "Module Code")]
        public string ModuleCode { get; set; }

        [BsonElement("AssessmentScores")]
        [Display(Name = "Assessments")]
        public Dictionary<string, double> AssessmentScores { get; set; }

        [BsonElement("OverallScore")]
        [Display(Name = "Overall Score")]
        public double FinalScore { get; set; }

        [BsonIgnore] 
        public Student StudentDetails { get; set; }

        public static Grade ParseFromCSV(string line, string moduleCode, List<Assessment> assessments)
        {
            var values = line.Trim().Split(',');

            if (values.Length >= assessments.Count + 1)
            {
                var grade = new Grade
                {
                    Id = values[0],
                    ModuleCode = moduleCode,
                    AssessmentScores = new Dictionary<string, double>()
                };

                for (int i = 1; i <= assessments.Count; i++)
                {
                    if (double.TryParse(values[i], out double score))
                    {
                        grade.AssessmentScores[assessments[i - 1].AssessmentName] = score;
                    }
                    else
                    {
                        // Handle invalid score value
                    }
                }

                return grade;
            }

            return null; // Skip invalid lines
        }

        public class DuplicateRow
        {
            public int RowNumber { get; set; }
            public string StudentId { get; set; }
        }

    }
}



//public double CalculateFinalScore(Module module)
//{
//    double finalScore = 0.0;

//    // Iterate over the assessments in the module
//    foreach (var assessment in module.Assessments)
//    {
//        var current = assessment.AssessmentName;
//        if (AssessmentScores.TryGetValue(current, out double score))
//        {
//            // Calculate the weighted score for the assessment
//            double scores = score * assessment.Weightage / 100.0;

//            // Add the weighted score to the total
//            finalScore += scores;
//        }
//    }

//    FinalScore = Math.Round(finalScore, 2);
//    return FinalScore;
//}   