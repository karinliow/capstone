using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace capstone_mongo.Models
{
    public class Student
    {
        [BsonId]
        //[BsonRepresentation(BsonType.ObjectId)]
        [Display(Name = "Student ID (SIT)")]
        public string StudentId { get; set; }

        [BsonElement]
        [Display(Name = "Student Name")]
        public string Name { get; set; }

        [BsonElement]
        public List<string> ModuleCodes { get; set; }
    }

}