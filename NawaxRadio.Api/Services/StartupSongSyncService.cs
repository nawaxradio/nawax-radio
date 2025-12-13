using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NawaxRadio.Api.Services
{
    public class StartupSongSyncService : BackgroundService
    {
        private readonly IFirestoreSongRepository _repo;
        private readonly ISongService _songService;
        private readonly ILogger<StartupSongSyncService> _logger;

        public StartupSongSyncService(
            IFirestoreSongRepository repo,
            ISongService songService,
            ILogger<StartupSongSyncService> logger)
        {
            _repo = repo;
            _songService = songService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("StartupSongSyncService: syncing Firestore -> InMemory ...");

                var songs = await _repo.GetAllAsync(stoppingToken);

                int added = 0;
                foreach (var s in songs)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var exists = _songService.GetById(s.Id);
                    if (exists == null)
                    {
                        _songService.Add(s);
                        added++;
                    }
                }

                _logger.LogInformation("StartupSongSyncService: sync done. loaded={Count}, added={Added}", songs.Count, added);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartupSongSyncService: sync failed");
            }
        }
    }
}
