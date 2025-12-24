// Services/StartupSongSyncService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NawaxRadio.Api.Services
{
    public class StartupSongSyncService : BackgroundService
    {
        private readonly ILogger<StartupSongSyncService> _logger;
        private readonly IFirestoreSongRepository _repo;
        private readonly ISongService _songService;

        public StartupSongSyncService(
            ILogger<StartupSongSyncService> logger,
            IFirestoreSongRepository repo,
            ISongService songService)
        {
            _logger = logger;
            _repo = repo;
            _songService = songService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("🔁 StartupSongSyncService started...");

                var fetched = await _repo.GetAllAsync(stoppingToken);

                int upserted = 0;
                foreach (var s in fetched)
                {
                    if (s == null) continue;
                    _songService.Add(s); // upsert in InMemorySongService
                    upserted++;
                }

                _logger.LogInformation("✅ StartupSongSyncService done. fetched={fetched} upserted={upserted} inMemoryActive={count}",
                    fetched?.Count ?? 0,
                    upserted,
                    _songService.GetAll().Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ StartupSongSyncService failed.");
            }
        }
    }
}
