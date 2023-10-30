using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using capstone_mongo.Services;
using capstone_mongo.Models;
using capstone_mongo.Helper;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;


namespace capstone_mongo.Controllers
{

    public class ModuleController : Controller
    {
        private readonly ModuleService moduleService;

        public ModuleController(ModuleService moduleService)
        {
            this.moduleService = moduleService;
        }

        // GET: Module
        public IActionResult Index()
        {
            return View(moduleService.GetAllModules());
        }

        public IActionResult Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Module module = moduleService.GetModule(id);
            if (module == null)
            {
                return NotFound();
            }

            return View(module);
        }

        [HttpGet]
        public IActionResult AddModule()
        {
            // Initialize the Assessments list with a blank assessment
            var moduleViewModel = new Module
            {
                Assessments = new List<Assessment> { new Assessment() }
            };

            return View(moduleViewModel);
        }

        // POST: Add New Module
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModule(Module module)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var assessments = new List<Assessment>();

                    foreach (var assessment in module.Assessments)
                    {
                        if (assessment.PeerWeightage > 0)
                        {
                            assessment.PeerEvaluation = true;
                        }
                        var newAssessment = new Assessment
                        {
                            AssessmentName = assessment.AssessmentName,
                            MaxScore = assessment.MaxScore,
                            Weightage = assessment.Weightage,
                            PeerEvaluation = assessment.PeerEvaluation,
                            PeerWeightage = assessment.PeerWeightage
                        };

                        assessments.Add(newAssessment);
                    }

                    // Assign the assessments list to the module's Assessments property
                    module.Assessments = assessments;

                    // Save the module to MongoDB
                    await moduleService.AddAsync(module);

                    TempData["ModuleSuccess"] = $"Module {module.ModuleCode} created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                catch (DuplicateModuleException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    ViewBag.ShowValidationSummary = true;
                    return View(module);
                }
            }
            else
            {
                ViewBag.ShowValidationSummary = true; // Set the flag to indicate to show the validation 
                return View(module);
            }
        }

        // GET: Module/Edit/<id>
        [HttpGet]
        public ActionResult Edit(string id)
        {
            Module module = moduleService.GetModule(id);
            if (module == null)
            {
                return NotFound();
            }

            return View(module);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Module updatedModule)
        {
            try
            {
                // Retrieve the existing module from the database
                Module module = moduleService.GetModule(id);

                // Update the assessments
                for (int i = 0; i < module.Assessments.Count; i++)
                {
                    module.Assessments[i].AssessmentName = updatedModule.Assessments[i].AssessmentName;
                    module.Assessments[i].Weightage = updatedModule.Assessments[i].Weightage;
                    module.Assessments[i].PeerEvaluation = updatedModule.Assessments[i].PeerEvaluation;
                    module.Assessments[i].PeerWeightage = updatedModule.Assessments[i].PeerWeightage;
                    module.Assessments[i].MaxScore = updatedModule.Assessments[i].MaxScore;
                }

                if (User.IsInRole("admin"))
                {
                    if (updatedModule.ModuleName != null)
                    {
                        // Update the module properties
                        module.ModuleName = updatedModule.ModuleName;
                        await moduleService.UpdateAdminAsync(module);
                    }

                    TempData["ModuleSuccess"] = $"Module details for {module.ModuleCode} updated successfully.";
                }
                else
                {
                    // Save the changes to the database
                    await moduleService.UpdateAsync(module);
                    TempData["ModuleSuccess"] = "Assessment details updated successfully.";
                }

                return RedirectToAction(nameof(Details), new { id = module.ModuleCode });

            }
            catch
            {
                return View(updatedModule);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string moduleCode)
        {
            string message;

            try
            {
                Module module = moduleService.GetModule(moduleCode);
                await moduleService.DeleteAsync(module);
                message = "Module deleted successfully!";
                TempData["ModuleSuccess"] = message;
                return Ok(message);
            }
            catch
            {
                message = "Failed to delete module.";
                return BadRequest(message);
            }
        }
    }
}