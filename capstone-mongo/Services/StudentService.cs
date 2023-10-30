using MongoDB.Driver;
using capstone_mongo.Models;
using capstone_mongo.Helper;

namespace capstone_mongo.Services
{
    public class StudentService
    {
        private readonly IMongoCollection<Student> students;

        public StudentService(IServiceProvider sp)
        {
            IMongoDatabase database = MongoConfig.GetDatabase(sp);
            students = database.GetCollection<Student>("Student");
        }

        public List<Student> GetStudentsByModule(string moduleCode)
        {
            var filter = Builders<Student>.Filter.Eq("ModuleCodes", moduleCode);
            var result = students.Find(filter).ToList();
            return result;
        }

        public async Task<List<Student>> GetStudentsByModuleAsync(string moduleCode)
        {
            var filter = Builders<Student>.Filter.AnyEq("ModuleCodes", moduleCode);
            return await students.Find(filter).ToListAsync();
        }

        public async Task<Student> GetStudentAsync(string studentId)
        {
            return await students.Find(s => s.StudentId.Equals(studentId))
                                 .FirstOrDefaultAsync();
        }

    }

}

