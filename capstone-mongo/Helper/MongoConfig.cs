using System;
using capstone_mongo.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace capstone_mongo.Helper
{
    public static class MongoConfig
    {
        private static IMongoDatabase _db;
        private static bool _userLoggedIn;

        public static bool IsUserLoggedIn()
        {
            return _userLoggedIn;
        }

        public static void SetLoginStatus(bool userLoggedIn)
        {
            _userLoggedIn = userLoggedIn;
        }

        public static void Initialize(IMongoDatabase database, bool userLoggedIn)
        {
            _db = database;
            _userLoggedIn = userLoggedIn;
        }

        public static IMongoDatabase GetDatabase(IServiceProvider serviceProvider)
        {
            if (_userLoggedIn)
            {
                return _db;
            }

            else
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = configuration.GetConnectionString("MONGODB_URI");
                string databaseName = "UsersDB";

                MongoClient client = new MongoClient(connectionString);
                return client.GetDatabase(databaseName);
            }
        }

        public static IMongoDatabase GetUserDatabase()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json")
                   .Build();

            string connectionString = configuration.GetConnectionString("MONGODB_URI");
            string databaseName = "UsersDB";

            MongoClient client = new MongoClient(connectionString);
            return client.GetDatabase(databaseName);
        }

        public static IMongoCollection<T> GetMongoCollection<T>(
            IMongoDatabase db, string value)
        {
            string collection = value;

            if (collection != null && collection.ToLower() != "users")
            {
                collection = $"{value}_{typeof(T).Name}s";

                var allCollections = db.ListCollectionNames().ToList();
                var isExist = allCollections.FirstOrDefault(name => name.Equals(collection));

                if (isExist == null)
                {
                    db.CreateCollection(collection);
                }
            }
            return db.GetCollection<T>(collection);
        }
    }
}


