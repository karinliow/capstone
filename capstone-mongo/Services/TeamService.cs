using MongoDB.Driver;
using capstone_mongo.Models;
using capstone_mongo.Helper;
using System.Linq;
using MongoDB.Bson;

namespace capstone_mongo.Services
{
    public class TeamService
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<Team> teams;

        private readonly SessionService sessionService;

        public TeamService(IServiceProvider sp,
                           SessionService sessionService)
        {
            this.sessionService = sessionService;

            if (!MongoConfig.IsUserLoggedIn())
            {
                // Redirect to "No Access" page or perform any other action
                throw new AccessDenied("Access denied. User is not logged in.");
            }

            database = MongoConfig.GetDatabase(sp);
            teams = MongoConfig.GetMongoCollection<Team>(database, sessionService.ModuleCode);
        }

        public async Task<string> GetGroupAsync(string sid)
        {
            var filter = Builders<Team>.Filter.AnyEq(x => x.Students, sid);
            var group = await teams.Find(filter).Project(x => x.Group).FirstOrDefaultAsync();

            if (group != null)
            {
                return group;
            }
            return null;
        }


    }

}
