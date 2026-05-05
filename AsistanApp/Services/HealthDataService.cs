using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsistanApp.Services 
{
    public class HealthDataService
    {
        public object GetElderlySession(string token) => new { Name = "Test", Email = "test@test.com", Id = 1 };
        public List<object> GetFamilyMembers(int id) => new List<object>();
        public List<object> GetHealthRecords(int id, object type = null) => new List<object>();
        public object AuthenticateElderly(string user, string pass) => new { Token = "dummy", User = new { Id = 1, Name = "Buse" } };
        public void AddHealthRecord(object record, string note = null, object value = null) { }
        public object GetUserState(int id) => new { State = "Stable" };
        public object SetUserState(int id, object state) => new { Success = true };
    }
}