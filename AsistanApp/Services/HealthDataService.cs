using System.Collections.Generic;

namespace AsistanApp.Services 
{
    public class HealthDataService
    {
        public dynamic GetElderlySession(string token) => new { Name = "Test", Email = "test@test.com" };
        public List<object> GetFamilyMembers(int id) => new List<object>();
        public List<object> GetHealthRecords(int id, object type = null) => new List<object>();
        public bool? AuthenticateElderly(string user, string pass) => true;
        public void AddHealthRecord(object record, string note = null, object value = null) { }
    }
}