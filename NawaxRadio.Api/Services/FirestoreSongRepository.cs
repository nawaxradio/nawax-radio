using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    public class FirestoreSongRepository : IFirestoreSongRepository
    {
        private readonly ILogger<FirestoreSongRepository> _logger;
        private readonly FirestoreDb _db;
        private readonly string _collection;

        public FirestoreSongRepository(IConfiguration config, ILogger<FirestoreSongRepository> logger)
        {
            _logger = logger;

            // ✅ Collection name (default: songs)
            _collection = Environment.GetEnvironmentVariable("FIRESTORE_SONGS_COLLECTION")
                          ?? config["Firestore:SongsCollection"]
                          ?? "songs";

            // ✅ ProjectId (اول env، بعد config، بعد fallback)
            var projectId =
                Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
                ?? Environment.GetEnvironmentVariable("GCLOUD_PROJECT")
                ?? config["Firebase:ProjectId"]
                ?? config["GoogleCloud:ProjectId"];

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new InvalidOperationException(
                    "ProjectId is missing. Set GOOGLE_CLOUD_PROJECT or Firebase:ProjectId."
                );
            }

            _db = FirestoreDb.Create(projectId);

            _logger.LogInformation(
                "FirestoreSongRepository initialized. projectId={ProjectId}, collection={Collection}",
                projectId, _collection
            );
        }

        public async Task<List<Song>> GetAllAsync(CancellationToken ct)
        {
            try
            {
                var col = _db.Collection(_collection);

                // اگر فقط Active ها رو می‌خوای:
                // var snap = await col.WhereEqualTo("isActive", true).GetSnapshotAsync(ct);

                var snap = await col.GetSnapshotAsync(ct);

                var list = new List<Song>(snap.Count);

                foreach (var doc in snap.Documents)
                {
                    if (ct.IsCancellationRequested) break;

                    var song = Map(doc);
                    if (song != null)
                        list.Add(song);
                }

                _logger.LogInformation("Firestore GetAllAsync done. fetched={Count}", list.Count);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore GetAllAsync failed.");
                return new List<Song>();
            }
        }

        // ✅ ct اختیاری شد (مشکل UploadEndpoints حل شد)
        public async Task SaveAsync(Song song, CancellationToken ct = default)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));

            try
            {
                // اگر Id خالی بود، بساز
                if (string.IsNullOrWhiteSpace(song.Id))
                    song.Id = Guid.NewGuid().ToString();

                // Defaults
                song.CreatedAt = song.CreatedAt == default ? DateTime.UtcNow : song.CreatedAt;
                song.Mood ??= new List<string>();
                song.Tags ??= new List<string>();

                // ✅ اگر از Upload/Firestore نیومده بود، فعالش کن
                // (اگر عمداً false گذاشته باشی، بهتره از همونجا false بیاد)
                if (song.IsActive == false)
                    song.IsActive = true;

                var col = _db.Collection(_collection);
                var doc = col.Document(song.Id);

                var data = new Dictionary<string, object?>
                {
                    ["name"] = song.Name ?? "",
                    ["singer"] = song.Singer ?? "",
                    ["type"] = song.Type ?? "",
                    ["year"] = song.Year,
                    ["lengthSec"] = song.LengthSec,
                    ["audioUrl"] = song.AudioUrl ?? "",
                    ["isActive"] = song.IsActive,
                    ["mood"] = song.Mood,
                    ["tags"] = song.Tags,
                    ["createdAt"] = song.CreatedAt
                };

                await doc.SetAsync(data, cancellationToken: ct);

                _logger.LogInformation("Firestore SaveAsync OK. id={Id}", song.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore SaveAsync failed. id={Id}", song?.Id);
                throw;
            }
        }

        private Song? Map(DocumentSnapshot doc)
        {
            if (doc == null || !doc.Exists) return null;

            var d = doc.ToDictionary();

            string GetStr(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (d.TryGetValue(k, out var v) && v != null)
                        return v.ToString() ?? "";
                }
                return "";
            }

            int GetInt(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (d.TryGetValue(k, out var v) && v != null)
                    {
                        if (v is int i) return i;
                        if (v is long l) return (int)l;
                        if (int.TryParse(v.ToString(), out var p)) return p;
                    }
                }
                return 0;
            }

            bool GetBoolDefaultTrue(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (d.TryGetValue(k, out var v) && v != null)
                    {
                        if (v is bool b) return b;
                        if (bool.TryParse(v.ToString(), out var p)) return p;
                    }
                }
                // ✅ اگر فیلد نبود: TRUE
                return true;
            }

            DateTime GetDateUtc(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (d.TryGetValue(k, out var v) && v != null)
                    {
                        if (v is Timestamp ts) return ts.ToDateTime().ToUniversalTime();
                        if (v is DateTime dt) return dt.ToUniversalTime();
                        if (DateTime.TryParse(v.ToString(), out var parsed))
                            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                    }
                }
                return DateTime.UtcNow;
            }

            List<string> GetStringList(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (d.TryGetValue(k, out var v) && v != null)
                    {
                        if (v is IEnumerable<object> arr)
                            return arr.Select(x => x?.ToString() ?? "")
                                      .Where(x => !string.IsNullOrWhiteSpace(x))
                                      .ToList();

                        if (v is IEnumerable<string> sarr)
                            return sarr.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    }
                }
                return new List<string>();
            }

            var song = new Song
            {
                Id = doc.Id,
                Name = GetStr("name", "title", "songName"),
                Singer = GetStr("singer", "artist"),
                Type = GetStr("type", "genre"),
                Year = GetInt("year"),
                LengthSec = GetInt("lengthSec", "duration", "durationSec"),
                AudioUrl = GetStr("audioUrl", "url", "streamUrl"),
                IsActive = GetBoolDefaultTrue("isActive", "active"),
                Mood = GetStringList("mood", "moods"),
                Tags = GetStringList("tags"),
                CreatedAt = GetDateUtc("createdAt", "created_at", "created")
            };

            if (string.IsNullOrWhiteSpace(song.AudioUrl))
            {
                _logger.LogWarning("Song {Id} has empty AudioUrl (doc={DocId})", song.Id, doc.Id);
            }

            return song;
        }
    }
}
