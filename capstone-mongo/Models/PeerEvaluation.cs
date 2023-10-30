using System;
using capstone_mongo.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace capstone_mongo.Models
{

    public class PeerEvaluation
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("Assessment")]
        public string Assessment { get; set; }

        [BsonElement("EvaluatorId")]
        public string EvaluatorId { get; set; }

        [BsonElement("Group")]
        public string Group { get; set; }

        [BsonElement("Evaluated")]
        public List<Evaluated> EvaluatedMembers { get; set; }

        [BsonElement("EvaluationScore")]
        public double EvaluationScore { get; set; }

        public class Evaluated
        {
            [BsonElement("Student")]
            public string Student { get; set; }

            [BsonElement("Score")]
            public int Score { get; set; }
        }
    }
}

