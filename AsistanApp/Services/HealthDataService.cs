using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsistanApp.Services
{
    public class HealthDataService
    {
        public async Task<dynamic> GetElderlySession(string token) => await Task.FromResult<dynamic>(new { Id = 1, Name = "Buse" });
        public async Task<List<dynamic>> GetFamilyMembers(int id) => await Task.FromResult(new List<dynamic>());
        public async Task<List<dynamic>> GetHealthRecords(int id, string type = null) => await Task.FromResult(new List<dynamic>());
        public async Task<dynamic> AuthenticateElderly(string user, string pass) => await Task.FromResult<dynamic>(new { Token = "dummy", User = new { Id = 1 } });
        public async Task AddHealthRecord(object record, string note = null, int? value = null) => await Task.CompletedTask;
        public async Task<dynamic> GetUserState(int id) => await Task.FromResult<dynamic>(new { State = "Active" });
        public async Task SetUserState(int id, object state) => await Task.CompletedTask;
    }

    public class AuthService
    {
        public async Task<bool> ValidateToken(string token) => await Task.FromResult(true);
    }
}