using capstone_mongo.Models;
using capstone_mongo.Services;
using Microsoft.AspNetCore.Mvc;
using System.Configuration;
using capstone_mongo.Helper;
using System.Diagnostics;
using static capstone_mongo.Models.Grade;

namespace capstone_mongo.Controllers
{

    public class GradeController : Controller
    {
        private readonly SessionService sessionService;
        private readonly GradeService gradeService;
        private readonly FileService fileService;
        private readonly StudentService studentService;

        private readonly IConfiguration config;

        public GradeController(SessionService sessionService,
                               GradeService gradeService,
                               FileService fileService,
                               StudentService studentService,
                               IConfiguration config)
        {
            this.sessionService = sessionService;
            this.gradeService = gradeService;
            this.fileService = fileService;
            this.studentService = studentService;
            this.config = config;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                string moduleCode = sessionService.ModuleCode;
                if (moduleCode != null)
                {
                    await gradeService.PopulateAssessmentsAsync(moduleCode);
                }
                var grades = gradeService.GetAllGrades();
                return View(grades);
            }
            catch (AccessDenied)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

        }
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Grade grade = gradeService.Get(id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var student = gradeService.Get(id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Grade updatedGrade)
        {

            try
            {
                Grade getGrade = gradeService.Get(id);

                // Update the assessment scores
                foreach (var assessmentScore in updatedGrade.AssessmentScores)
                {
                    getGrade.AssessmentScores[assessmentScore.Key] = assessmentScore.Value;
                }

                await gradeService.UpdateAsync(getGrade);
                await gradeService.CalculateGradesAsync();
                TempData["GradeSuccess"] = "Grade details updated successfully.";

                return RedirectToAction(nameof(Details), new { id = getGrade.Id });

            }
            catch
            {
                return View(updatedGrade);
            }

        }

        public void ExportToCsv()
        {
            try
            {
                var module = sessionService.ModuleCode;
                var gradeList = gradeService.GetAllGrades();

                var exportedDate = DateTime.Today.ToString("yyyyMMdd");
                var filename = $"{module}_Grades_{exportedDate}.csv";

                // Get file path from config
                var filePath = config.GetSection("ExportSettings:FilePath").Value;

                // create dir if it does not exist
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                fileService.ExportGradesToCsv(gradeList, module,
                                              Path.Combine(filePath, filename));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        // GET: Load upload file page
        [HttpGet]
        public IActionResult UploadFile()
        {
            var module = sessionService.ModuleCode;
            var exportedDate = DateTime.Now.ToString("yyyyMMdd");
            ViewBag.ExpectedFilename = $"{module}_Grades_{exportedDate}.csv";
            return View();
        }

        // POST: Save uploaded file
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                var module = sessionService.ModuleCode;
                var exportedDate = DateTime.Now.ToString("yyyyMMdd");
                var expectedFilename = $"{module}_Grades_{exportedDate}.csv";

                if (file == null || file.FileName != expectedFilename)
                {
                    TempData["icon"] = "fa fa-remove";
                    TempData["message"] = "Invalid file! Please upload a file with the correct filename.";
                    TempData["class"] = "alert alert-danger";
                    return RedirectToAction("UploadFile");
                }

                await fileService.SaveUploadedFile(file);

                var (grades, success, alert, message, skippedRows) = await fileService.ReadAndProcessCSV(file);

                if (success == false)
                {
                    if (message != null)
                    {
                        TempData["icon"] = "fa fa-remove";
                        TempData["message"] = "Failed to process CSV!";
                        TempData["class"] = "alert alert-danger";
                    }
                }
                
                else
                {
                    await gradeService.BulkInsertOrUpdate(grades);
                    await gradeService.CalculateGradesAsync();

                    string successMessage = "Successfully uploaded file! All grades have been updated.";

                    if (!string.IsNullOrEmpty(alert))
                    {
                        if (skippedRows.Any())
                        {
                            string[] records = message.Split(new string[] { "CSV Row " }, StringSplitOptions.RemoveEmptyEntries);
                            List<string> recList = new List<string>();

                            foreach (var row in skippedRows)
                            {
                                recList.Add($"<li>{row}</li>");
                            }

                            var skippedRowsMsg = string.Join("\n", recList);

                            TempData["icon"] = "fa fa-exclamation-circle";
                            TempData["message"] = $"{successMessage}";
                            TempData["note"] = $"{message}\n\n{skippedRowsMsg}";

                        }
                        else
                        {
                            TempData["message"] = successMessage;
                        }
                        TempData["class"] = "alert alert-warning";
                    }
                    else
                    {
                        TempData["message"] = successMessage;
                        TempData["icon"] = "fa fa-check";
                        TempData["class"] = "alert alert-success";
                    }
                }
            }
            catch (InvalidFileException ex)
            {
                TempData["icon"] = "fa fa-remove"; 
                TempData["message"] = ex.Message;
                TempData["class"] = "alert alert-danger";
            }
            catch (ServiceException ex)
            {
                TempData["icon"] = "fa fa-remove";
                if (ex.Message.Contains("duplicate"))
                {                    
                    string[] duplicateRows = ex.Message.Split(new string[] { "CSV Row " }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> errorList = new List<string>();

                    foreach (var row in duplicateRows.Skip(1))
                    {
                        errorList.Add($"<li>CSV Row {row}</li>");
                    }

                    var errorMsg = ex.Message.Split(".")[0];
                    string errorMsgList = $"{errorMsg}.\n{string.Join("\n", errorList)}";

                    TempData["message"] = errorMsgList;
                    TempData["note"] = "These rows have been saved into the error log file. Please check your CSV file and re-upload after modification.";
                }
                else
                {
                    TempData["message"] = ex.Message;
                }
                TempData["class"] = ex.AlertClass;
            }
            catch (Exception)
            {
                TempData["icon"] = "fa fa-remove";
                TempData["message"] = "An error occurred while processing the file.";
                TempData["class"] = "alert alert-danger";
            }

            return RedirectToAction("UploadFile");

        }

    }

}