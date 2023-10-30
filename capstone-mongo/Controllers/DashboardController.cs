using System;
using capstone_mongo.Models;
using capstone_mongo.Services;
using Microsoft.AspNetCore.Mvc;
using static capstone_mongo.Models.Chart;

namespace capstone_mongo.Controllers
{
	public class DashboardController : Controller
	{
        private readonly SessionService sessionService;
        private readonly GradeService gradeService;

        public DashboardController(SessionService sessionService,
                               GradeService gradeService)
        {
            this.sessionService = sessionService;
            this.gradeService = gradeService;
        }

        public IActionResult Index()
        {
            var minData = gradeService.GetMinAssignment();
            var maxData = gradeService.GetMaxAssignment();

            var meanData = gradeService.CalcMeanAssignment();
            var medianData = gradeService.GetMedianAssignment();
            var modeData = gradeService.GetModeAssignment();

            var stdDeviationData = gradeService.CalcStdDevAssignment();
            var assignments = gradeService.GetAssignment();

            var chartModel = new Chart
            {
                Assignments = assignments,
                MeanData = meanData.Select((mean, index) => new ChartData
                {
                    Assignment = assignments[index],
                    Mean = mean
                }).ToList(),
                MedianData = medianData.Select((median, index) => new ChartData
                {
                    Assignment = assignments[index],
                    Median = median
                }).ToList(),
                ModeData = modeData.Select((mode, index) => new ChartData
                {
                    Assignment = assignments[index],
                    Mode = mode
                }).ToList(),
                StandardDeviationData = stdDeviationData.Select((stdDeviation, index) => new ChartData
                {
                    Assignment = assignments[index],
                    StandardDeviation = stdDeviation
                }).ToList(),
                MaxData = maxData.Select((max, index) => new ChartData
                {
                    Assignment = assignments[index],
                    Max = max
                }).ToList(),
                MinData = minData.Select((min, index) => new ChartData
                {
                    Assignment = assignments[index],
                    Min = min
                }).ToList(),
                BellCurveXValues = gradeService.GetOverallGrades(),
                BellCurveYValues = gradeService.GetBellCurveData()
            };

            return View(chartModel);
        }
    }
}

