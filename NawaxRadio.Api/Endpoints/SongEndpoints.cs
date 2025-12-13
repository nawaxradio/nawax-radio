using NawaxRadio.Api.Domain;
using NawaxRadio.Api.Services;

namespace NawaxRadio.Api.Endpoints;

public static class SongEndpoints
{
    public static IEndpointRouteBuilder MapSongEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/songs");

        // GET /songs
        group.MapGet("/", (ISongService service) =>
        {
            var songs = service.GetAll();
            return Results.Ok(songs);
        });

        // GET /songs/{id}
        group.MapGet("/{id}", (string id, ISongService service) =>
        {
            var song = service.GetById(id);
            return song is null ? Results.NotFound() : Results.Ok(song);
        });

        // GET /songs/by-channel/{slug}
        group.MapGet("/by-channel/{slug}", (
            string slug,
            ISongService songService,
            IChannelService channelService) =>
        {
            var channel = channelService.GetBySlug(slug);

            if (channel is null)
                return Results.NotFound($"Channel '{slug}' not found");

            var songs = songService.GetByChannel(channel);
            return Results.Ok(songs);
        });

        // POST /songs
        group.MapPost("/", (Song song, ISongService service) =>
        {
            var created = service.Add(song);
            return Results.Created($"/songs/{created.Id}", created);
        });

        // DELETE /songs/{id}
        group.MapDelete("/{id}", (string id, ISongService service) =>
        {
            var ok = service.Delete(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
