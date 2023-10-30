using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace capstone_mongo.Models
{
	public class User
	{
		[BsonId]
		[BsonElement("_id")]
		[Display(Name = "Username")]
        [Required(ErrorMessage = "Please enter a valid username!")]
        public string UserId { get; set; }

        [BsonElement("Password")]
        [Required(ErrorMessage = "Please enter a valid password!")]
        public string Password { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Module")]
        public string Module { get; set; }

        [BsonElement("Role")]
        public string Role { get; set; }
	}
}

