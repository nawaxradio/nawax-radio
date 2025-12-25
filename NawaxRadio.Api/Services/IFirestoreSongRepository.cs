using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    public interface IFirestoreSongRepository
    {
        Task<List<Song>> GetAllAsync(CancellationToken cancellationToken);
        Task SaveAsync(Song song, CancellationToken cancellationToken = default);
    }
}
