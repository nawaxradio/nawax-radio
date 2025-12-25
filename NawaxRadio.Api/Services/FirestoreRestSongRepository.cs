// Services/FirestoreRestSongRepository.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    public class FirestoreRestSongRepository : IFirestoreSongRepository
    {
        private readonly ILogger<FirestoreRestSongRepository> _logger;
        private readonly HttpClient _http;

        private readonly string _projectId;
        private readonly string _collection;

        public FirestoreRestSongRepository(
            ILogger<FirestoreRestSongRepository> logger,
            IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _http = httpFactory.CreateClient(nameof(FirestoreRestSongRepository));

            _projectId = (Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
                          ?? Environment.GetEnvironmentVariable("GCLOUD_PROJECT")
                          ?? "").Trim();

            _collection = (Environment.GetEnvironmentVariable("FIRESTORE_SONGS_COLLECTION")
                           ?? "songs").Trim();

            _logger.LogInformation("FirestoreRestSongRepository initialized. projectId={projectId}, collection={collection}",
                _projectId, _collection);
        }

        // =====================================================
        // ✅ GetAll via RunQuery
        // =====================================================
        public async Task<List<Song>> GetAllAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_projectId))
                return new List<Song>();

            try
            {
                var token = await GetAccessTokenAsync(cancellationToken);

                var url =
                    $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

                var payload = new
                {
                    structuredQuery = new
                    {
                        from = new[]
                        {
                            new { collectionId = _collection }
                        },
                        limit = 500
                    }
                };

                var json = JsonSerializer.Serialize(payload);

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var res = await _http.SendAsync(req, cancellationToken);
                var body = await res.Content.ReadAsStringAsync(cancellationToken);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Firestore REST RunQuery failed. status={status} body={body}",
                        (int)res.StatusCode, body);
                    return new List<Song>();
                }

                return ParseRunQuerySongs(body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore REST GetAllAsync (RunQuery) failed.");
                return new List<Song>();
            }
        }

        // =====================================================
        // ✅ FIX: Save via POST create document
        // =====================================================
        public async Task SaveAsync(Song song, CancellationToken cancellationToken = default)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));
            if (string.IsNullOrWhiteSpace(_projectId)) throw new InvalidOperationException("Missing projectId");

            if (string.IsNullOrWhiteSpace(song.Id))
                song.Id = Guid.NewGuid().ToString();

            if (song.CreatedAt == default)
                song.CreatedAt = DateTime.UtcNow;

            var token = await GetAccessTokenAsync(cancellationToken);

            // ✅ Create document:
            // POST .../documents/{collection}?documentId={id}
            var url =
                $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{_collection}?documentId={Uri.EscapeDataString(song.Id)}";

            var json = JsonSerializer.Serialize(BuildDocumentPayload(song));

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, cancellationToken);
            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Firestore REST Create failed. status={status} body={body}",
                    (int)res.StatusCode, body);

                // ✅ make it obvious to /debug/seed-firestore that it failed
                throw new Exception($"Firestore REST Create failed: {(int)res.StatusCode} {body}");
            }
        }

        // =====================================================
        // ✅ Debug helper: list top-level collection IDs
        // =====================================================
        public async Task<List<string>> ListTopCollectionsAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_projectId))
                return new List<string>();

            var token = await GetAccessTokenAsync(cancellationToken);

            var url =
                $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:listCollectionIds";

            var payload = new
            {
                // parent is root documents
                parent = $"projects/{_projectId}/databases/(default)/documents",
                pageSize = 100
            };

            var json = JsonSerializer.Serialize(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, cancellationToken);
            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Firestore REST listCollectionIds failed. status={status} body={body}",
                    (int)res.StatusCode, body);
                return new List<string>();
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("collectionIds", out var ids) || ids.ValueKind != JsonValueKind.Array)
                return new List<string>();

            var list = new List<string>();
            foreach (var x in ids.EnumerateArray())
            {
                var s = x.GetString();
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
            }
            return list;
        }

        // -----------------------------
        // Parsers + Field helpers
        // -----------------------------

        private static List<Song> ParseRunQuerySongs(string json)
        {
            var list = new List<Song>();

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("document", out var d)) continue;
                if (!d.TryGetProperty("fields", out var fields)) continue;

                var id = GetString(fields, "id");
                if (string.IsNullOrWhiteSpace(id))
                    id = ExtractDocId(d);

                var s = new Song
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    Name = GetString(fields, "name") ?? "",
                    Singer = GetString(fields, "singer") ?? "",
                    Year = GetInt(fields, "year") ?? 0,
                    Type = GetString(fields, "type") ?? "unknown",
                    LengthSec = GetInt(fields, "lengthSec") ?? 0,
                    Mood = GetStringArray(fields, "mood"),
                    Tags = GetStringArray(fields, "tags"),
                    AudioUrl = GetString(fields, "audioUrl") ?? "",
                    CoverUrl = GetString(fields, "coverUrl") ?? "",
                    UploadedBy = GetString(fields, "uploadedBy") ?? "",
                    IsJingle = GetBool(fields, "isJingle") ?? false,
                    Language = GetString(fields, "language") ?? "fa",
                    IsActive = GetBool(fields, "isActive") ?? true,
                    BitrateKbps = GetInt(fields, "bitrateKbps"),
                    FileSizeBytes = GetLong(fields, "fileSizeBytes"),
                };

                var createdAt = GetTimestamp(fields, "createdAt");
                if (createdAt.HasValue) s.CreatedAt = createdAt.Value;

                list.Add(s);
            }

            return list;
        }

        private static string? ExtractDocId(JsonElement document)
        {
            if (!document.TryGetProperty("name", out var nameEl)) return null;
            var full = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(full)) return null;
            var parts = full.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[^1] : null;
        }

        private static object BuildDocumentPayload(Song s)
        {
            return new
            {
                fields = new Dictionary<string, object?>
                {
                    ["id"] = new { stringValue = s.Id ?? "" },
                    ["name"] = new { stringValue = s.Name ?? "" },
                    ["singer"] = new { stringValue = s.Singer ?? "" },
                    ["year"] = new { integerValue = s.Year.ToString() },
                    ["type"] = new { stringValue = s.Type ?? "unknown" },
                    ["lengthSec"] = new { integerValue = s.LengthSec.ToString() },

                    ["mood"] = StringArrayField(s.Mood),
                    ["tags"] = StringArrayField(s.Tags),

                    ["audioUrl"] = new { stringValue = s.AudioUrl ?? "" },
                    ["coverUrl"] = new { stringValue = s.CoverUrl ?? "" },

                    ["createdAt"] = new
                    {
                        timestampValue = (s.CreatedAt.Kind == DateTimeKind.Utc
                            ? s.CreatedAt
                            : s.CreatedAt.ToUniversalTime()).ToString("O")
                    },

                    ["uploadedBy"] = new { stringValue = s.UploadedBy ?? "" },
                    ["isJingle"] = new { booleanValue = s.IsJingle },
                    ["language"] = new { stringValue = s.Language ?? "fa" },
                    ["isActive"] = new { booleanValue = s.IsActive },

                    ["bitrateKbps"] = s.BitrateKbps.HasValue ? new { integerValue = s.BitrateKbps.Value.ToString() } : null,
                    ["fileSizeBytes"] = s.FileSizeBytes.HasValue ? new { integerValue = s.FileSizeBytes.Value.ToString() } : null,
                }
            };
        }

        private static object StringArrayField(IEnumerable<string>? values)
        {
            var list = new List<object>();
            if (values != null)
            {
                foreach (var s in values)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        list.Add(new { stringValue = s });
                }
            }

            return new
            {
                arrayValue = new
                {
                    values = list
                }
            };
        }

        private static string? GetString(JsonElement fields, string key)
        {
            if (!fields.TryGetProperty(key, out var v)) return null;
            if (v.TryGetProperty("stringValue", out var sv)) return sv.GetString();
            return null;
        }

        private static int? GetInt(JsonElement fields, string key)
        {
            if (!fields.TryGetProperty(key, out var v)) return null;
            if (v.TryGetProperty("integerValue", out var iv) && int.TryParse(iv.GetString(), out var i)) return i;
            return null;
        }

        private static long? GetLong(JsonElement fields, string key)
        {
            if (!fields.TryGetProperty(key, out var v)) return null;
            if (v.TryGetProperty("integerValue", out var iv) && long.TryParse(iv.GetString(), out var i)) return i;
            return null;
        }

        private static bool? GetBool(JsonElement fields, string key)
        {
            if (!fields.TryGetProperty(key, out var v)) return null;
            if (v.TryGetProperty("booleanValue", out var bv)) return bv.GetBoolean();
            return null;
        }

        private static List<string> GetStringArray(JsonElement fields, string key)
        {
            var list = new List<string>();
            if (!fields.TryGetProperty(key, out var v)) return list;

            if (!v.TryGetProperty("arrayValue", out var av)) return list;
            if (!av.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array) return list;

            foreach (var item in values.EnumerateArray())
            {
                if (item.TryGetProperty("stringValue", out var sv))
                {
                    var s = sv.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
                }
            }
            return list;
        }

        private static DateTime? GetTimestamp(JsonElement fields, string key)
        {
            if (!fields.TryGetProperty(key, out var v)) return null;
            if (v.TryGetProperty("timestampValue", out var tv))
            {
                if (DateTime.TryParse(tv.GetString(), out var dt))
                    return dt.ToUniversalTime();
            }
            return null;
        }

        private static async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            var path = (Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(path))
                path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "firebase-sa.json");

#pragma warning disable CS0618
            var cred = GoogleCredential.FromFile(path)
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
#pragma warning restore CS0618

            return await cred.UnderlyingCredential.GetAccessTokenForRequestAsync(null, ct);
        }
    }
}
