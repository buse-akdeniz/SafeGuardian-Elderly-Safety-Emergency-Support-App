using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsistanApp.Services 
{
    public class HealthDataService
    {
        public async Task<object> GetElderlySession(string token) => null;
        public async Task<List<object>> GetFamilyMembers(int id) => new List<object>();
        public async Task<List<object>> GetHealthRecords(int id) => new List<object>();
        public async Task<bool> AuthenticateElderly(string user, string pass) => true;
        public async Task AddHealthRecord(object record) { }
    }
}