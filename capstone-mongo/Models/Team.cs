using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace capstone_mongo.Models
{
    public class Team
    {

        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Group")]
        public string Group { get; set; }

        [BsonElement("Students")]
        public List<string> Students { get; set; }

    }
}


