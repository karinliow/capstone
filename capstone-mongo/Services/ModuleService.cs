using System;
using MongoDB.Driver;
using capstone_mongo.Models;
using capstone_mongo.Helper;
using MongoDB.Bson;

namespace capstone_mongo.Services
{
    public class ModuleService
    {
        private readonly IMongoCollection<Module> modules;

        public ModuleService(IServiceProvider sp)
        {
            IMongoDatabase database = MongoConfig.GetDatabase(sp);
            modules = database.GetCollection<Module>("Module");
        }

        public List<Module> GetAllModules()
        {
            return modules.Find(module => true).ToList();
        }

        public Module GetModule(string moduleCode)
        {
            return modules.Find(m => m.ModuleCode == moduleCode).FirstOrDefault();
        }

        public async Task<Module> GetModuleAsync(string moduleCode)
        {
            return await modules.Find(m => m.ModuleCode.Equals(moduleCode))
                                .FirstOrDefaultAsync();
        }

        public async Task<Module> AddAsync(Module module)
        {
            try
            {
                await modules.InsertOneAsync(module);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new DuplicateModuleException(module.ModuleCode);
            }
            return module;
        }

        public async Task<Module> UpdateAdminAsync(Module module)
        {
            var filter = Builders<Module>.Filter.Eq(m => m.ModuleCode, module.ModuleCode);

            try
            {
                var update = Builders<Module>.Update
                    .Set(m => m.ModuleName, module.ModuleName)
                    .Set(m => m.Assessments, module.Assessments);

                await modules.UpdateOneAsync(filter, update);
            }
            catch
            {
                throw new Exception("Failed to update module.");
            }

            return module;
        }

        public async Task<Module> UpdateAsync(Module module)
        {
            var filter = Builders<Module>.Filter.Eq(m => m.ModuleCode, module.ModuleCode);

            try
            {
                var update = Builders<Module>.Update
                    .Set(m => m.Assessments, module.Assessments);

                await modules.UpdateOneAsync(filter, update);
            }
            catch
            {
                throw new Exception("Failed to update module.");
            }

            return module;
        }

        public async Task<Module> DeleteAsync(Module module)
        {
            var filter = Builders<Module>.Filter.Eq(m => m.ModuleCode, module.ModuleCode);
            try
            {
                await modules.DeleteOneAsync(filter);
            }
            catch
            {
                throw new Exception("Module not deleted.");
            }
            return module;
        }
    }
}

