using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NawaxRadio.Api.Domain;
using NawaxRadio.Api.Services;
using System;
using System.Linq;

namespace NawaxRadio.Api.Endpoints;

public static class SongEndpoints
{
    public static IEndpointRouteBuilder MapSongEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/songs");

        // ✅ GET /songs (list)
        group.MapGet("", (ISongService service) =>
        {
            var songs = service.GetAll();
            return Results.Ok(songs);
        });

        // ✅ GET /songs/{id}
        group.MapGet("{id}", (string id, ISongService service) =>
        {
            var song = service.GetById(id);
            return song is null ? Results.NotFound() : Results.Ok(song);
        });

        // ✅ GET /songs/by-channel/{slug}
        group.MapGet("by-channel/{slug}", (
            string slug,
            ISongService songService,
            IChannelService channelService) =>
        {
            var channel = channelService.GetBySlug(slug);

            if (channel is null)
                return Results.NotFound(new { message = $"Channel '{slug}' not found" });

            var songs = songService.GetByChannel(channel);
            return Results.Ok(songs);
        });

        // ✅ GET /songs/decade/{decade}   (moved from UploadEndpoints)
        group.MapGet("decade/{decade}", (string decade, ISongService songService) =>
        {
            var all = songService.GetAll();

            var normalizedDecade = decade.Trim().ToLowerInvariant();

            var filtered = all.Where(s =>
            {
                if (s.Year <= 0) return false;

                var decadeStart = (s.Year / 10) * 10;
                var key = $"{decadeStart}s".ToLowerInvariant();
                return key == normalizedDecade;
            }).ToList();

            return Results.Ok(filtered);
        });

        // ✅ GET /songs/type/{type}      (moved from UploadEndpoints)
        group.MapGet("type/{type}", (string type, ISongService songService) =>
        {
            var all = songService.GetAll();
            var filtered = all
                .Where(s =>
                    !string.IsNullOrWhiteSpace(s.Type) &&
                    s.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Results.Ok(filtered);
        });

        // ✅ GET /songs/mood/{mood}      (moved from UploadEndpoints)
        group.MapGet("mood/{mood}", (string mood, ISongService songService) =>
        {
            var all = songService.GetAll();
            var filtered = all
                .Where(s =>
                    s.Mood != null &&
                    s.Mood.Any(m => m.Equals(mood, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Results.Ok(filtered);
        });

        // ✅ POST /songs
        group.MapPost("", (Song song, ISongService service) =>
        {
            var created = service.Add(song);
            return Results.Created($"/songs/{created.Id}", created);
        });

        // ✅ DELETE /songs/{id}
        group.MapDelete("{id}", (string id, ISongService service) =>
        {
            var ok = service.Delete(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
