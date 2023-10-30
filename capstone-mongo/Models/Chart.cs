using System;
namespace capstone_mongo.Models
{
	public class Chart
	{
        public List<string> Assignments { get; set; }
        public List<ChartData> MeanData { get; set; }
        public List<ChartData> MedianData { get; set; }
        public List<ChartData> ModeData { get; set; }
        public List<ChartData> StandardDeviationData { get; set; }
        public List<ChartData> MaxData { get; set; }
        public List<ChartData> MinData { get; set; }
        public List<double> BellCurveXValues { get; set; } // New property for bell curve x-values
        public List<double> BellCurveYValues { get; set; } // New property for bell curve y-values


        public class ChartData
        {
            public string Assignment { get; set; }
            public double Mean { get; set; }
            public double Median { get; set; }
            public double Mode { get; set; }
            public double StandardDeviation { get; set; }
            public double Max { get; set; }
            public double Min { get; set; }
        }
    }
}