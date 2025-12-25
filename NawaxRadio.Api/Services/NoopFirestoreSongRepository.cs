using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    public class NoopFirestoreSongRepository : IFirestoreSongRepository
    {
        public Task<List<Song>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<Song>());

        public Task SaveAsync(Song song, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
