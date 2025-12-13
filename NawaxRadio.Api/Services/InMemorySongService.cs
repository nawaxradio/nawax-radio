// Services/InMemorySongService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    public class InMemorySongService : ISongService
    {
        private readonly List<Song> _songs = new();

        public IEnumerable<Song> GetAll()
        {
            return _songs.Where(s => s.IsActive).ToList();
        }

        public Song? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return _songs.FirstOrDefault(s => s.Id == id);
        }

        public IEnumerable<Song> GetByChannel(Channel channel)
        {
            if (channel == null)
                return Enumerable.Empty<Song>();

            var filter = channel.Filter;
            var playlistConfig = channel.PlaylistConfig;

            IEnumerable<Song> query = _songs.Where(s => s.IsActive);

            if (filter != null)
            {
                if (filter.Type is { Count: > 0 })
                {
                    query = query.Where(s => filter.Type.Contains(s.Type));
                }

                if (filter.Mood is { Count: > 0 })
                {
                    query = query.Where(s => s.Mood.Any(m => filter.Mood.Contains(m)));
                }

                if (filter.Latest)
                {
                    query = query.OrderByDescending(s => s.CreatedAt);
                }

                if (filter.YearFrom.HasValue)
                {
                    query = query.Where(s => s.Year >= filter.YearFrom.Value);
                }

                if (filter.YearTo.HasValue)
                {
                    query = query.Where(s => s.Year <= filter.YearTo.Value);
                }
            }

            if (playlistConfig?.MaxSongs is > 0)
            {
                query = query.Take(playlistConfig.MaxSongs);
            }

            return query.ToList();
        }

        public Song Add(Song song)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song));

            if (string.IsNullOrWhiteSpace(song.Id))
            {
                song.Id = Guid.NewGuid().ToString();
            }

            song.CreatedAt = song.CreatedAt == default ? DateTime.UtcNow : song.CreatedAt;

            _songs.Add(song);
            return song;
        }

        public bool Delete(string id)
        {
            var song = GetById(id);
            if (song == null)
                return false;

            _songs.Remove(song);
            return true;
        }
    }
}
