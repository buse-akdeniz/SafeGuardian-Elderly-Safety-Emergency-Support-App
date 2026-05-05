using System.Collections.Generic;

namespace AsistanApp.Services 
{
    public class HealthDataService
    {
        public dynamic GetElderlySession(string token) => new { Name = "Test", Email = "test@test.com", Id = 1 };
        public List<object> GetFamilyMembers(int id) => new List<object>();
        public List<dynamic> GetHealthRecords(int id, object type = null) => new List<object>();
        public dynamic AuthenticateElderly(string user, string pass) => new { Token = "dummy-token", User = new { Id = 1, Name = "Buse" } };
        public void AddHealthRecord(object record, string note = null, object value = null) { }
        public dynamic GetUserState(int id) => new { State = "Stable" };
        public void SetUserState(int id, string state) { }
    }
}