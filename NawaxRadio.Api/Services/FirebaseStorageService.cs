// Services/FirebaseStorageService.cs
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NawaxRadio.Api.Options;

namespace NawaxRadio.Api.Services
{
    /// <summary>
    /// Uploads songs to Firebase Storage using Google ADC credentials.
    /// Works with older Google.Apis.Auth packages (no GoogleCredential.GetAccessTokenForRequestAsync).
    /// Also works even if FirebaseStorageOptions does not have a Bucket property (uses reflection + IConfiguration fallback).
    /// </summary>
    public class FirebaseStorageService : ICloudStorage
    {
        private readonly FirebaseStorageOptions _opt;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _cfg;

        public FirebaseStorageService(
            IOptions<FirebaseStorageOptions> opt,
            IHttpClientFactory httpFactory,
            IConfiguration cfg)
        {
            _opt = opt?.Value ?? new FirebaseStorageOptions();
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        // MUST match your interface: ICloudStorage.UploadSongAsync(Stream,string,string,CancellationToken)
        public async Task<string> UploadSongAsync(
            Stream fileStream,
            string originalFileName,
            string contentType,
            CancellationToken ct)
        {
            if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrWhiteSpace(originalFileName))
                originalFileName = "song.mp3";

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "audio/mpeg";

            // ✅ Get bucket safely (no hard dependency on FirebaseStorageOptions.Bucket)
            var bucket = ResolveBucketName(_opt, _cfg);
            if (string.IsNullOrWhiteSpace(bucket))
                throw new InvalidOperationException(
                    "Firebase bucket is not configured. Set it in appsettings.json under Firebase:{Bucket|StorageBucket|BucketName} " +
                    "or env var Firebase__Bucket."
                );

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            // Build object path
            var safeName = Path.GetFileName(originalFileName);
            var objectName = $"songs/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{safeName}";
            var encodedObject = Uri.EscapeDataString(objectName);

            // Upload endpoint (GCS JSON API)
            var uploadUrl =
                $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name={encodedObject}";

            // ADC credential
            GoogleCredential cred = await GoogleCredential.GetApplicationDefaultAsync(ct);

            // Scope for Storage RW
            if (cred.IsCreateScopedRequired)
            {
                cred = cred.CreateScoped(new[]
                {
                    "https://www.googleapis.com/auth/devstorage.read_write"
                });
            }

            // ✅ Token (compatible with older packages)
            var token = await GetAccessTokenCompatAsync(cred, ct);

            using var client = _httpFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl);

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            req.Content = content;

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Firebase Storage upload failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n" +
                    $"Bucket={bucket}\n" +
                    $"UploadUrl={uploadUrl}\n" +
                    $"ResponseBody={body}"
                );
            }

            // Public media URL (if bucket/object readable; otherwise use your /radio/.../stream proxy)
            var publicUrl =
                $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{encodedObject}?alt=media";

            return publicUrl;
        }

        private static string ResolveBucketName(FirebaseStorageOptions opt, IConfiguration cfg)
        {
            // 1) Try common option property names via reflection (because your Options class doesn't have Bucket)
            var candidates = new[]
            {
                "Bucket",
                "BucketName",
                "StorageBucket",
                "StorageBucketName",
                "FirebaseBucket",
                "FirebaseStorageBucket"
            };

            foreach (var name in candidates)
            {
                var prop = opt.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null) continue;

                var val = prop.GetValue(opt)?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                    return CleanBucket(val);
            }

            // 2) Fallback: read from configuration directly
            // Works with appsettings.json ("Firebase": { "Bucket": "..." }) or env var Firebase__Bucket
            var cfgCandidates = new[]
            {
                "Firebase:Bucket",
                "Firebase:BucketName",
                "Firebase:StorageBucket",
                "Firebase:StorageBucketName"
            };

            foreach (var key in cfgCandidates)
            {
                var v = cfg[key];
                if (!string.IsNullOrWhiteSpace(v))
                    return CleanBucket(v);
            }

            return "";
        }

        private static string CleanBucket(string bucket)
        {
            bucket = (bucket ?? "").Trim();

            // allow user mistakenly putting gs://
            if (bucket.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
                bucket = bucket.Substring("gs://".Length);

            // remove trailing slashes
            bucket = bucket.TrimEnd('/');

            return bucket;
        }

        private static async Task<string> GetAccessTokenCompatAsync(GoogleCredential cred, CancellationToken ct)
        {
            // Older packages: token access is usually on UnderlyingCredential as ITokenAccess
            var tokenAccess = cred.UnderlyingCredential as ITokenAccess;
            if (tokenAccess == null)
            {
                throw new InvalidOperationException(
                    "GoogleCredential.UnderlyingCredential does not implement ITokenAccess. " +
                    "Update Google.Apis.Auth package or ensure ADC is correctly configured."
                );
            }

            // ITokenAccess signature: GetAccessTokenForRequestAsync(string? authUri = null)
            // (No CancellationToken on older interface; we respect ct by checking before/after)
            ct.ThrowIfCancellationRequested();
            var token = await tokenAccess.GetAccessTokenForRequestAsync();
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Received empty access token from Google credentials.");

            return token;
        }
    }
}
