using System.Text;
using Amazon.Runtime.Internal.Util;
using capstone_mongo.Helper;
using capstone_mongo.Models;

namespace capstone_mongo.Services
{
    public class FileService
    {

        private readonly SessionService sessionService;
        private readonly ModuleService moduleService;
        private readonly StudentService studentService;
        private readonly IConfiguration config;

        public FileService(SessionService sessionService,
                           ModuleService moduleService,
                           StudentService studentService,
                           IConfiguration config)
        {
            this.sessionService = sessionService;
            this.moduleService = moduleService;
            this.studentService = studentService;
            this.config = config;
        }

        private bool checkIfCSV(string fileName)
        {
            var validExtensions = new[] { ".csv" };
            var fileExtension = Path.GetExtension(fileName).ToLower();

            return validExtensions.Contains(fileExtension);
        }

        public async Task<string> SaveUploadedFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                //return "failed";
                throw new InvalidFileException("No file was selected.");

            }

            else if (!checkIfCSV(file.FileName))
            {
                //return "invalid";
                throw new InvalidFileException("Invalid file format. Only CSV files are allowed.");

            }

            else
            {
                var path = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot",
                        file.FileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return "success";
            }
        }

        //public async Task<(IEnumerable<Grade> grades, string message, string alertClass)> ReadCSV(IFormFile uploaded)
        //{

        //    var gradeList = new List<Grade>();
        //    string message = null;
        //    string alertClass = null;

        //    Module module = await moduleService.GetModuleAsync(sessionService.ModuleCode);

        //    if (module == null || module.Assessments == null || module.Assessments.Count == 0)
        //    {
        //        message = "Please ensure that module assessments are declared.";
        //        alertClass = "alert alert-danger";
        //        return (gradeList, message, alertClass);
        //    }

        //    else
        //    {
        //        // open file and read contents in CSV
        //        using (var stream = uploaded.OpenReadStream())
        //        using (var reader = new StreamReader(stream))
        //        {

        //            // get assessments
        //            var assessments = module.Assessments.Select(a => a.AssessmentName).ToArray();

        //            // skips header
        //            reader.ReadLine();

        //            string line;
        //            while ((line = await reader.ReadLineAsync()) != null)
        //            {

        //                var grade = Grade.ParseFromCSV(line, sessionService.ModuleCode, assessments);

        //                if (grade != null)
        //                {
        //                    gradeList.Add(grade);
        //                }
        //            }
        //        }
        //    }

        //    return (gradeList, message, alertClass);
        //}

        public void ExportGradesToCsv(List<Grade> gradesList, string moduleCode, string exportPath)
        {
            var sb = new StringBuilder();

            string header = "Student ID";

            if (gradesList.Count > 0 && gradesList[0].AssessmentScores != null)
            {
                foreach (var assessmentName in gradesList[0].AssessmentScores.Keys)
                {
                    header += ", " + assessmentName;
                }
            }

            sb.AppendLine(header);

            // Filter grades by module code
            var filteredGrades = gradesList.Where(g => g.ModuleCode == moduleCode).ToList();

            foreach (var grade in filteredGrades)
            {
                var line = $"{grade.Id},";
                var assessmentScores = grade.AssessmentScores;

                foreach (var assessment in assessmentScores)
                {
                    line += $"{assessment.Value},";
                }

                line = line.TrimEnd(',');

                sb.AppendLine(line);
            }

            File.WriteAllText(exportPath, sb.ToString());
        }

        public void CSVLogFile(string logFileName, string logMessage)
        {
            string filePath = config.GetSection("ExportSettings:FilePath").Value;
            // Save the error message to a log file
            string logFilePath = Path.Combine(filePath, "ErrorLogs", logFileName);

            // Create the ErrorLogs directory within the same directory as specified by filePath
            Directory.CreateDirectory(Path.Combine(filePath, "ErrorLogs"));

            // Append to the log file if it already exists
            if (File.Exists(logFilePath))
            {
                File.AppendAllText(logFilePath, $"\n{DateTime.Now:h:mm tt}: {logMessage}\n");
            }
            else
            {
                File.WriteAllText(logFilePath, $"{DateTime.Now:h:mm tt}: {logMessage}\n");
            }
        }

        public async Task<(IEnumerable<Grade> grades, bool success, string alertClass, string errorMsg, List<string> skippedRows)> ReadAndProcessCSV(IFormFile uploaded)
        {
            var gradeList = new List<Grade>();
            string errorMsg = null;
            string alertClass = null;
            bool success = false;
            var skippedRows = new List<string>(); // List to store skipped rows


            Module module = await moduleService.GetModuleAsync(sessionService.ModuleCode);

            if (module == null || module.Assessments == null || module.Assessments.Count == 0)
            {
                errorMsg = "Please ensure that module assessments are declared.";
                alertClass = "alert alert-danger";
                throw new ServiceException(errorMsg, alertClass);
            }

            // open file and read contents in CSV
            using (var stream = uploaded.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                // Get the header row from the CSV
                var headerLine = await reader.ReadLineAsync();
                var expectedHeader = GenerateExpectedHeader(module.Assessments);
                if (headerLine != expectedHeader)
                {
                    errorMsg = "There is something wrong with the file you uploaded. " +
                        "\nIf this error persists, please download the file again before re-uploading.";
                    alertClass = "alert alert-danger";
                    throw new ServiceException(errorMsg, alertClass);
                }

                var students = await studentService.GetStudentsByModuleAsync(sessionService.ModuleCode);
                var existingStudentIds = students.Select(s => s.StudentId).ToHashSet();

                var duplicateRows = new List<Grade.DuplicateRow>();

                string logFileName = $"{module.ModuleCode}_{DateTime.Now:yyyyMMdd}_error.log";

                int rowNumber = 2;
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var grade = Grade.ParseFromCSV(line, sessionService.ModuleCode, module.Assessments);

                    if (grade != null)
                    {
                        // Check if student exists in the module
                        if (!existingStudentIds.Contains(grade.Id))
                        {
                            errorMsg = "The following records of the students in your CSV was not processed as they do not exist in our system for this module.";
                            alertClass = "alert alert-warning";
                            var logMessage = $"Student {grade.Id} does not exist for this module. This record was skipped during processing.";
                            CSVLogFile(logFileName, $"[STUDENT {grade.Id} NOT FOUND]\n{logMessage}");

                            // add to list
                            skippedRows.Add($"CSV Row {rowNumber}: {grade.Id}"); 
                            rowNumber++;
                            continue;
                        }

                        // add duplicated records to list
                        if (gradeList.Any(g => g.Id == grade.Id))
                        {
                            duplicateRows.Add(new Grade.DuplicateRow { RowNumber = rowNumber, StudentId = grade.Id });
                            rowNumber++;
                            continue;
                        }

                        gradeList.Add(grade);
                    }

                    rowNumber++;
                }

                // for duplicate records
                if (duplicateRows.Any())
                {
                    // Construct the error message with duplicate rows
                    errorMsg = "There are duplicate records of the following students in your CSV.\n";
                    var logMessage = new StringBuilder();

                    foreach (var duplicateRow in duplicateRows)
                    {
                        errorMsg += $"CSV Row {duplicateRow.RowNumber}: {duplicateRow.StudentId}\n";
                        logMessage.Append($"CSV Row {duplicateRow.RowNumber}: {duplicateRow.StudentId}\n");
                    }

                    alertClass = "alert alert-danger";

                    CSVLogFile(logFileName, $"[DUPLICATES FOUND IN CSV]\n{logMessage}");

                    throw new ServiceException(errorMsg, alertClass);
                }

                success = true;
            }

            return (gradeList, success, alertClass, errorMsg, skippedRows);
        }

        private string GenerateExpectedHeader(List<Assessment> assessments)
        {
            var headerBuilder = new StringBuilder("Student ID");

            foreach (var assessment in assessments)
            {
                headerBuilder.Append(", ").Append(assessment.AssessmentName);
            }

            return headerBuilder.ToString();
        }
    }
}

