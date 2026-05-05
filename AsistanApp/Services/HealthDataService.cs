using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsistanApp.Services 
{
    public class HealthDataService
    {
        public async Task<dynamic> GetElderlySession(string token) => new { Name = "Test", Email = "test@test.com" };
        public async Task<List<object>> GetFamilyMembers(int id) => new List<object>();
        public async Task<List<object>> GetHealthRecords(int id, string type = null) => new List<object>();
        public async Task<bool?> AuthenticateElderly(string user, string pass) => true;
        public async Task AddHealthRecord(object record, string note = null, int? value = null) { }
    }
}