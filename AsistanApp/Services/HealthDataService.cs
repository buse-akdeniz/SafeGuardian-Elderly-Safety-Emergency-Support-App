using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AsistanApp.Services
{
    public class ElderlyProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public int? Age { get; set; }
    }

    public class FamilyProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int ElderlyId { get; set; }
    }

    public class HealthDataService
    {
        private static readonly ConcurrentDictionary<string, ElderlyProfile> ElderlyByToken = new();
        private static readonly ConcurrentDictionary<int, ElderlyProfile> ElderlyById = new();
        private static readonly ConcurrentDictionary<string, FamilyProfile> FamilyByToken = new();
        private static readonly ConcurrentDictionary<string, int> FamilyEmailToElderlyUserId =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly string DataDirectory = Environment.GetEnvironmentVariable("DATA_DIR")
            ?? Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/tmp", ".safeguardian");
        private static readonly string StorePath = Path.Combine(DataDirectory, "app-data.json");
        private static readonly object StoreLock = new();
        private static bool _loaded;

        static HealthDataService()
        {
            LoadFromDisk();
            EnsureReviewFamilyLinks();
        }

        private static void EnsureReviewFamilyLinks()
        {
            const int demoElderlyId = 999;
            var reviewFamilyEmails = new[]
            {
                "family1.app-review-elderly-001@vitaguard.app",
                "review.family@safeguardian.app"
            };

            foreach (var email in reviewFamilyEmails)
            {
                if (!FamilyEmailToElderlyUserId.ContainsKey(email))
                {
                    FamilyEmailToElderlyUserId[email] = demoElderlyId;
                }
            }

            if (!ElderlyById.ContainsKey(demoElderlyId))
            {
                var profile = new ElderlyProfile
                {
                    Id = demoElderlyId,
                    Name = "Test User",
                    Email = "review.elderly@safeguardian.app"
                };
                ElderlyById[demoElderlyId] = profile;
                ElderlyByToken["elder-999"] = profile;
                ElderlyByToken["demo-review-elderly-expired-token"] = profile;
            }
        }

        private sealed class PersistedAppData
        {
            public Dictionary<int, ElderlyProfile> ElderlyProfiles { get; set; } = new();
            public Dictionary<string, int> FamilyLinks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        public static void LoadFromDisk()
        {
            lock (StoreLock)
            {
                if (_loaded) return;
                _loaded = true;
                try
                {
                    if (!File.Exists(StorePath)) return;
                    var json = File.ReadAllText(StorePath);
                    var data = JsonSerializer.Deserialize<PersistedAppData>(json);
                    if (data == null) return;

                    foreach (var pair in data.ElderlyProfiles)
                    {
                        ElderlyById[pair.Key] = pair.Value;
                        ElderlyByToken[$"elder-{pair.Key}"] = pair.Value;
                    }

                    foreach (var pair in data.FamilyLinks)
                    {
                        FamilyEmailToElderlyUserId[pair.Key] = pair.Value;
                    }
                }
                catch
                {
                    // Keep in-memory defaults if disk read fails.
                }
            }
        }

        public static void SaveToDisk()
        {
            lock (StoreLock)
            {
                try
                {
                    Directory.CreateDirectory(DataDirectory);
                    var payload = new PersistedAppData
                    {
                        ElderlyProfiles = ElderlyById.ToDictionary(pair => pair.Key, pair => pair.Value),
                        FamilyLinks = FamilyEmailToElderlyUserId.ToDictionary(pair => pair.Key, pair => pair.Value)
                    };
                    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(StorePath, json);
                }
                catch
                {
                    // Ignore persistence errors; in-memory state still works.
                }
            }
        }

        public static int StableUserId(string email)
        {
            return Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode((email ?? "").Trim()));
        }

        public static void LinkFamilyEmailToElderly(string familyEmail, int elderlyUserId)
        {
            if (string.IsNullOrWhiteSpace(familyEmail) || elderlyUserId <= 0) return;
            FamilyEmailToElderlyUserId[familyEmail.Trim()] = elderlyUserId;
            SaveToDisk();
        }

        private static int TryResolveReviewFamilyElderlyId(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return 0;
            var normalized = email.Trim();
            if (normalized.Contains("app-review-elderly", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("review.family@safeguardian.app", StringComparison.OrdinalIgnoreCase))
            {
                EnsureReviewFamilyLinks();
                return 999;
            }
            return 0;
        }

        public static ElderlyProfile? GetElderlyProfile(int elderlyUserId)
        {
            return ElderlyById.TryGetValue(elderlyUserId, out var profile) ? profile : null;
        }

        public static void UpsertElderlyProfile(ElderlyProfile profile)
        {
            if (profile == null || profile.Id <= 0) return;
            ElderlyById[profile.Id] = profile;
            ElderlyByToken[$"elder-{profile.Id}"] = profile;
            SaveToDisk();
        }

        private static ElderlyProfile EnsureElderlyProfile(string email)
        {
            var id = StableUserId(email);
            if (ElderlyById.TryGetValue(id, out var existing))
            {
                return existing;
            }

            var displayName = email.Contains('@')
                ? email.Split('@')[0]
                : email;
            var profile = new ElderlyProfile
            {
                Id = id,
                Name = displayName,
                Email = email.Trim()
            };
            ElderlyById[id] = profile;
            ElderlyByToken[$"elder-{id}"] = profile;
            return profile;
        }

        public async Task<dynamic?> AuthenticateElderly(string user, string pass)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                return await Task.FromResult<dynamic?>(null);
            }

            var profile = EnsureElderlyProfile(user);
            var token = $"elder-{profile.Id}";
            return await Task.FromResult<dynamic?>(new
            {
                HasValue = true,
                Value = new { Token = token, User = profile }
            });
        }

        public dynamic? AuthenticateFamily(string user, string pass)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                return null;
            }

            var familyId = StableUserId(user);
            var token = $"family-{familyId}";
            var elderlyId = FamilyEmailToElderlyUserId.TryGetValue(user.Trim(), out var linked)
                ? linked
                : 0;

            if (elderlyId <= 0)
            {
                elderlyId = TryResolveReviewFamilyElderlyId(user);
                if (elderlyId > 0)
                {
                    LinkFamilyEmailToElderly(user.Trim(), elderlyId);
                }
            }

            var member = new FamilyProfile
            {
                Id = familyId,
                Name = user.Contains('@') ? user.Split('@')[0] : user,
                Email = user.Trim(),
                ElderlyId = elderlyId
            };
            FamilyByToken[token] = member;

            return new
            {
                HasValue = true,
                Value = new
                {
                    Token = token,
                    Member = new
                    {
                        member.Id,
                        member.Name,
                        Email = member.Email,
                        ElderlyId = elderlyId
                    }
                }
            };
        }

        public async Task<dynamic?> GetElderlySession(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return await Task.FromResult<dynamic?>(null);
            }

            if (ElderlyByToken.TryGetValue(token, out var profile))
            {
                return await Task.FromResult<dynamic?>(profile);
            }

            if (token.StartsWith("elder-", StringComparison.Ordinal) &&
                int.TryParse(token.AsSpan(6), out var id) &&
                ElderlyById.TryGetValue(id, out profile))
            {
                return await Task.FromResult<dynamic?>(profile);
            }

            return await Task.FromResult<dynamic?>(null);
        }

        public dynamic? GetFamilySession(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            if (FamilyByToken.TryGetValue(token, out var profile))
            {
                return profile;
            }

            if (token.StartsWith("family-", StringComparison.Ordinal) &&
                int.TryParse(token.AsSpan(7), out var id))
            {
                return FamilyByToken.Values.FirstOrDefault(f => f.Id == id);
            }

            return null;
        }

        public async Task<List<dynamic>> GetFamilyMembers(int id) =>
            await Task.FromResult(new List<dynamic>());

        public async Task<List<dynamic>> GetHealthRecords(int id, string? type = null) =>
            await Task.FromResult(new List<dynamic>());

        public async Task AddHealthRecord(object record, string? note = null, int? value = null) =>
            await Task.CompletedTask;

        public async Task<dynamic> GetUserState(int id) =>
            await Task.FromResult<dynamic>(new
            {
                Id = id,
                CurrentContext = "Home",
                ActiveTaskId = 0,
                ScreenPriority = 1,
                IsAssistantActive = true,
                UpdatedAt = DateTime.Now
            });

        public async Task SetUserState(int id, object state) => await Task.CompletedTask;
    }
}
