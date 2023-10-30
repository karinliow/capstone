using MongoDB.Driver;
using capstone_mongo.Models;
using capstone_mongo.Helper;
using BCrypt.Net;
using System.Security.Authentication;

namespace capstone_mongo.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> users;

        public UserService(IServiceProvider sp)
        {
            IMongoDatabase database = MongoConfig.GetDatabase(sp);
            users = MongoConfig.GetMongoCollection<User>(database, "Users");
        }

        public async Task<User> GetUserAsync(string user)
        {
            return await users.Find(u => u.UserId.Equals(user)).FirstOrDefaultAsync();
        }

        public async Task<User> AuthenticateUser(string UserId, string Password)
        {
            var res = await GetUserAsync(UserId);

            if (res != null)
            {
                string pw = res.Password;

                Password = Password.Trim();
                bool verify = BCrypt.Net.BCrypt.Verify(Password, pw);

                if (verify)
                {
                    return res;
                }
                else
                {
                    // Invalid password
                    throw new InvalidCredentialException("Invalid password");
                }
            }
            else
            {
                // Invalid username
                throw new InvalidCredentialException("Invalid username");
            }
        }
    }

}

