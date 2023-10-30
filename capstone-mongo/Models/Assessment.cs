using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace capstone_mongo.Models
{
    public class Assessment
    {
        [BsonIgnore]
        public int Index { get; set; }

        [BsonElement("AssessmentName")]
        [Required(ErrorMessage = "Assessment Name cannot be empty!")]
        public string AssessmentName { get; set; }

        [BsonElement("MaxScore")]
        [Required(ErrorMessage = "Max Score cannot be empty!")]
        //[RegularExpression(@"^([0-9]|[1-9][0-9]|100)$",
        //    ErrorMessage = "Max Score must be between 0 and 100.")]
        public int MaxScore { get; set; }

        [BsonElement("Weightage")]
        [Required(ErrorMessage = "Weightage cannot be empty!")]
        public double Weightage { get; set; }

        [BsonElement("PeerEvaluation")]
        public bool PeerEvaluation { get; set; }

        [BsonElement("PeerWeightage")]
        //[RegularExpression("^[1-9]\\d", ErrorMessage = "Peer Weightage must be more than 0")]
        public double PeerWeightage { get; set; }
    }
}

