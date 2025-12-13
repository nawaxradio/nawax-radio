// Endpoints/RadioEndpoints.cs
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using NawaxRadio.Api.Domain;
using NawaxRadio.Api.Services;

namespace NawaxRadio.Api.Endpoints
{
    public static class RadioEndpoints
    {
        private static readonly ConcurrentDictionary<string, LinkedList<string>> LastPlayedByChannel = new();
        private static readonly ConcurrentDictionary<string, int> PlayCountByChannel = new();

        private const int HistorySize = 5;
        private const int PlaysPerJingle = 5;

        public static IEndpointRouteBuilder MapRadioEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/radio")
                           .WithTags("Radio");

            group.MapGet("/{channel}/stream", HandleStream);

            return app;
        }

        private static IResult HandleStream(string channel, ISongService songService)
        {
            var allSongs = songService.GetAll()
                .Where(s => s.IsActive && !string.IsNullOrWhiteSpace(s.AudioUrl))
                .ToList();

            if (allSongs.Count == 0)
            {
                return Results.Problem("No songs available", statusCode: 503);
            }

            var shouldPlayJingle = ShouldPlayJingle(channel);

            var candidates = FilterSongsForChannel(allSongs, channel, shouldPlayJingle).ToList();

            if (candidates.Count == 0)
            {
                candidates = FilterSongsForChannel(allSongs, channel, shouldPlayJingle: false).ToList();

                if (candidates.Count == 0)
                {
                    candidates = allSongs;
                }
            }

            var picked = PickNonRepeatedSong(channel, candidates);

            if (picked is null || string.IsNullOrWhiteSpace(picked.AudioUrl))
            {
                return Results.Problem("No suitable song found", statusCode: 503);
            }

            RegisterPlay(channel, picked);

            return Results.Ok(new
            {
                audioUrl = picked.AudioUrl,
                songId = picked.Id,
                name = picked.Name,
                singer = picked.Singer,
                channel,
                isJingle = picked.IsJingle
            });
        }

        private static IEnumerable<Song> FilterSongsForChannel(
            IReadOnlyList<Song> allSongs,
            string channel,
            bool shouldPlayJingle)
        {
            var baseQuery = allSongs.AsEnumerable();

            if (shouldPlayJingle)
            {
                baseQuery = baseQuery.Where(s => s.IsJingle);
            }
            else
            {
                baseQuery = baseQuery.Where(s => !s.IsJingle);
            }

            channel = channel.ToLowerInvariant();

            return channel switch
            {
                "ghery" => baseQuery.Where(s =>
                    s.Mood.Contains("ghery") || s.Mood.Contains("blue") || s.Mood.Contains("dep")),

                "party" => baseQuery.Where(s =>
                    s.Mood.Contains("party")),

                "genz" => baseQuery.Where(s =>
                    s.Mood.Contains("genz") ||
                    s.Type == "trap" ||
                    s.Type == "modern"),

                "rap" => baseQuery.Where(s =>
                    s.Type == "rap" || s.Type == "hiphop"),

                "bandari" => baseQuery.Where(s =>
                    s.Type == "bandari" || s.Type == "jonobi"),

                "dep" => baseQuery.Where(s =>
                    s.Mood.Contains("dep") || s.Mood.Contains("blue")),

                "energy" => baseQuery.Where(s =>
                    s.Mood.Contains("energy")),

                "latest" => baseQuery
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(200),

                "main" or _ => baseQuery
            };
        }

        private static Song? PickNonRepeatedSong(string channel, IList<Song> candidates)
        {
            var lastPlayed = LastPlayedByChannel.GetOrAdd(channel, _ => new LinkedList<string>());

            var filtered = candidates
                .Where(s => !lastPlayed.Contains(s.AudioUrl))
                .ToList();

            if (filtered.Count == 0)
            {
                filtered = candidates.ToList();
            }

            if (filtered.Count == 0)
                return null;

            var index = Random.Shared.Next(filtered.Count);
            return filtered[index];
        }

        private static bool ShouldPlayJingle(string channel)
        {
            var current = PlayCountByChannel.AddOrUpdate(
                channel,
                addValueFactory: _ => 1,
                updateValueFactory: (_, prev) => prev + 1);

            return current % PlaysPerJingle == 0;
        }

        private static void RegisterPlay(string channel, Song song)
        {
            var lastPlayed = LastPlayedByChannel.GetOrAdd(channel, _ => new LinkedList<string>());

            if (!string.IsNullOrWhiteSpace(song.AudioUrl))
            {
                lastPlayed.AddFirst(song.AudioUrl);

                while (lastPlayed.Count > HistorySize)
                {
                    lastPlayed.RemoveLast();
                }
            }
        }
    }
}
