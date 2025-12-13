// Services/FirestoreSongRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.Options;
using NawaxRadio.Api.Domain;
using NawaxRadio.Api.Options;

namespace NawaxRadio.Api.Services
{
    public interface IFirestoreSongRepository
    {
        Task SaveAsync(Song song, CancellationToken cancellationToken = default);
        Task<List<Song>> GetAllAsync(CancellationToken cancellationToken = default);
    }

    public class FirestoreSongRepository : IFirestoreSongRepository
    {
        private readonly FirestoreDb _db;

        public FirestoreSongRepository(IOptions<FirebaseStorageOptions> options)
        {
            var opt = options.Value;

            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(opt.CredentialsPath))
            {
                credential = GoogleCredential.FromFile(opt.CredentialsPath);
            }
            else
            {
                credential = GoogleCredential.GetApplicationDefault();
            }

            var clientBuilder = new FirestoreClientBuilder
            {
                Credential = credential
            };

            var firestoreClient = clientBuilder.Build();

            _db = FirestoreDb.Create(opt.ProjectId, firestoreClient);
        }

        public async Task SaveAsync(Song song, CancellationToken cancellationToken = default)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));

            if (string.IsNullOrWhiteSpace(song.Id))
                song.Id = Guid.NewGuid().ToString();

            if (song.CreatedAt == default)
                song.CreatedAt = DateTime.UtcNow;

            var doc = new Dictionary<string, object?>
            {
                ["id"] = song.Id,
                ["name"] = song.Name,
                ["singer"] = song.Singer,
                ["year"] = song.Year,
                ["type"] = song.Type,
                ["lengthSec"] = song.LengthSec,
                ["mood"] = song.Mood,
                ["tags"] = song.Tags,
                ["audioUrl"] = song.AudioUrl,
                ["coverUrl"] = song.CoverUrl,
                ["language"] = song.Language,
                ["isJingle"] = song.IsJingle,
                ["isActive"] = song.IsActive,
                ["bitrateKbps"] = song.BitrateKbps,
                ["fileSizeBytes"] = song.FileSizeBytes,
                ["uploadedBy"] = song.UploadedBy,
                ["decade"] = GetDecade(song.Year),
                ["createdAt"] = Timestamp.FromDateTime(song.CreatedAt.ToUniversalTime())
            };

            await _db.Collection("songs")
                     .Document(song.Id)
                     .SetAsync(doc, cancellationToken: cancellationToken);
        }

        public async Task<List<Song>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var snap = await _db.Collection("songs").GetSnapshotAsync(cancellationToken);

            var list = new List<Song>();

            foreach (var doc in snap.Documents)
            {
                var d = doc.ToDictionary();

                string GetStr(string key) =>
                    d.TryGetValue(key, out var v) ? (v?.ToString() ?? "") : "";

                int GetInt(string key)
                {
                    if (!d.TryGetValue(key, out var v) || v == null) return 0;
                    if (v is long l) return (int)l;
                    if (v is int i) return i;
                    if (int.TryParse(v.ToString(), out var x)) return x;
                    return 0;
                }

                bool GetBool(string key, bool def = false)
                {
                    if (!d.TryGetValue(key, out var v) || v == null) return def;
                    if (v is bool b) return b;
                    if (bool.TryParse(v.ToString(), out var x)) return x;
                    return def;
                }

                long? GetLongNullable(string key)
                {
                    if (!d.TryGetValue(key, out var v) || v == null) return null;
                    if (v is long l) return l;
                    if (v is int i) return i;
                    if (long.TryParse(v.ToString(), out var x)) return x;
                    return null;
                }

                int? GetIntNullable(string key)
                {
                    if (!d.TryGetValue(key, out var v) || v == null) return null;
                    if (v is long l) return (int)l;
                    if (v is int i) return i;
                    if (int.TryParse(v.ToString(), out var x)) return x;
                    return null;
                }

                List<string> GetStringList(string key)
                {
                    if (!d.TryGetValue(key, out var v) || v == null) return new List<string>();
                    if (v is IEnumerable<object> arr)
                    {
                        var res = new List<string>();
                        foreach (var item in arr)
                            res.Add(item?.ToString() ?? "");
                        return res;
                    }
                    return new List<string>();
                }

                DateTime createdAt = DateTime.UtcNow;
                if (d.TryGetValue("createdAt", out var createdObj) && createdObj != null)
                {
                    // ✅ اینجا مشکل CS0077 حل شد: as نزن
                    if (createdObj is Timestamp ts)
                        createdAt = ts.ToDateTime().ToUniversalTime();
                    else if (DateTime.TryParse(createdObj.ToString(), out var dt))
                        createdAt = dt.ToUniversalTime();
                }

                var song = new Song
                {
                    Id = GetStr("id"),
                    Name = GetStr("name"),
                    Singer = GetStr("singer"),
                    Year = GetInt("year"),
                    Type = GetStr("type"),
                    LengthSec = GetInt("lengthSec"),
                    Mood = GetStringList("mood"),
                    Tags = GetStringList("tags"),
                    AudioUrl = GetStr("audioUrl"),
                    CoverUrl = GetStr("coverUrl"),
                    Language = GetStr("language"),
                    IsJingle = GetBool("isJingle", false),
                    IsActive = GetBool("isActive", true),
                    BitrateKbps = GetIntNullable("bitrateKbps"),
                    FileSizeBytes = GetLongNullable("fileSizeBytes"),
                    UploadedBy = GetStr("uploadedBy"),
                    CreatedAt = createdAt
                };

                if (string.IsNullOrWhiteSpace(song.Id))
                    song.Id = doc.Id;

                list.Add(song);
            }

            return list;
        }

        private static string GetDecade(int year)
        {
            if (year <= 0) return "unknown";
            var decadeStart = (year / 10) * 10;
            return $"{decadeStart}s";
        }
    }
}
