// Services/InMemorySongService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    /// <summary>
    /// Runtime in-memory cache for songs.
    /// IMPORTANT:
    /// - No default seeding (so we never force sample.mp3).
    /// - Songs are expected to be synced from Firestore via StartupSongSyncService or /debug/sync.
    /// </summary>
    public class InMemorySongService : ISongService
    {
        private readonly List<Song> _songs = new();

        public InMemorySongService()
        {
            // ✅ NO automatic seed here.
            // If you ever need a seed song for local testing, use:
            // SEED_TEST_SONG=true and call /debug/seed-inmemory
        }

        public IEnumerable<Song> GetAll()
        {
            return _songs.Where(s => s.IsActive).ToList();
        }

        public Song? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return _songs.FirstOrDefault(s => s.Id == id && s.IsActive);
        }

        public IEnumerable<Song> GetByChannel(Channel channel)
        {
            if (channel == null)
                return Enumerable.Empty<Song>();

            var key = (channel.Key ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key))
                return Enumerable.Empty<Song>();

            // ✅ Main channel = all active songs (acts as "mix")
            if (key == "main")
            {
                return _songs
                    .Where(s => s.IsActive)
                    .ToList();
            }

            return _songs
                .Where(s => s.IsActive)
                .Where(s => s.Mood != null &&
                            s.Mood.Any(m => (m ?? "").Trim().ToLowerInvariant() == key))
                .ToList();
        }

        public Song Add(Song song)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song));

            if (string.IsNullOrWhiteSpace(song.Id))
                song.Id = Guid.NewGuid().ToString();

            if (song.CreatedAt == default)
                song.CreatedAt = DateTime.UtcNow;

            // ✅ Upsert behavior: avoid duplicates if same Id comes again from Firestore sync
            var existingIndex = _songs.FindIndex(x => x.Id == song.Id);
            if (existingIndex >= 0)
                _songs[existingIndex] = song;
            else
                _songs.Add(song);

            return song;
        }

        public bool Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var s = _songs.FirstOrDefault(x => x.Id == id);
            if (s == null)
                return false;

            _songs.Remove(s);
            return true;
        }
    }
}
