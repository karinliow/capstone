using System;
using MongoDB.Driver;
using capstone_mongo.Models;
using capstone_mongo.Helper;
using System.Linq;

namespace capstone_mongo.Services
{
    public class PeerEvalService
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<PeerEvaluation> peerEvals;

        private readonly SessionService sessionService;
        private readonly TeamService teamService;

        public PeerEvalService(IServiceProvider sp, SessionService sessionService)
        {
            this.sessionService = sessionService;
            this.teamService = teamService;

            if (!MongoConfig.IsUserLoggedIn())
            {
                // Redirect to "No Access" page or perform any other action
                throw new InvalidOperationException("Access denied. User is not logged in.");
            }

            database = MongoConfig.GetDatabase(sp);
            peerEvals = MongoConfig.GetMongoCollection<PeerEvaluation>(database, sessionService.ModuleCode);

        }

        public async Task<double> CalculatePeerEvaluationAsync(string studentId, string assessment, string group)
        {

            var filter = Builders<PeerEvaluation>.Filter.And(
                Builders<PeerEvaluation>.Filter.Eq(x => x.Assessment, assessment),
                Builders<PeerEvaluation>.Filter.Eq(x => x.Group, group)
                );

            var evaluationScores = await peerEvals.Find(filter).ToListAsync();

            var totalScore = 0;
            var count = 0;

            foreach (var evaluation in evaluationScores)
            {
                var evaluated = evaluation.EvaluatedMembers.FirstOrDefault(x => x.Student == studentId);

                if (evaluated != null)
                {
                    totalScore += evaluated.Score;
                    count++;
                }
            }

            if (count > 0)
            {
                var averageScore = (double)totalScore / count;
                return averageScore;
            }

            return 0.0;
        }
    }
}

