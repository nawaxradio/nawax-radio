using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using NawaxRadio.Api.Options;

namespace NawaxRadio.Api.Services
{
    public class FirebaseStorageService : ICloudStorage
    {
        private readonly StorageClient _storageClient;
        private readonly FirebaseStorageOptions _options;

        public FirebaseStorageService(IOptions<FirebaseStorageOptions> options)
        {
            _options = options.Value;

            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(_options.CredentialsPath))
            {
                credential = GoogleCredential.FromFile(_options.CredentialsPath);
            }
            else
            {
                credential = GoogleCredential.GetApplicationDefault();
            }

            _storageClient = StorageClient.Create(credential);
        }

        public async Task<string> UploadSongAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var objectName = $"songs/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}";

            await _storageClient.UploadObjectAsync(
                _options.BucketName,
                objectName,
                contentType,
                fileStream,
                cancellationToken: cancellationToken);

            var publicUrl =
                $"https://firebasestorage.googleapis.com/v0/b/{_options.BucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";

            return publicUrl;
        }
    }
}
