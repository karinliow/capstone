using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace capstone_mongo.Models
{
    public class Module
    {
        [BsonId]
        [Display(Name = "Module Code")]
        [Required(ErrorMessage = "Module Code cannot be empty!")]
        public string ModuleCode { get; set; }

        [BsonElement("ModuleName")]
        [Display(Name = "Module Name")]
        [Required(ErrorMessage = "Module Name cannot be empty!")]
        public string ModuleName { get; set; }

        [BsonElement("AssignGroup")]
        [Display(Name = "Any Group Assignments? ")]
        public bool AssignGroup { get; set; }

        [BsonElement("Assessments")]
        [Required(ErrorMessage = "Please declare at least 1 assessment!")]
        public List<Assessment> Assessments { get; set; }

        public string AssignGroupDisplay => AssignGroup ? "Yes" : "No";

    }
}

