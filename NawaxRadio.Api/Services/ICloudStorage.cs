using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NawaxRadio.Api.Services
{
    public interface ICloudStorage
    {
        Task<string> UploadSongAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);
    }
}
