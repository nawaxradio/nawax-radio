// Endpoints/ChannelEndpoints.cs
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using NawaxRadio.Api.Services;

namespace NawaxRadio.Api.Endpoints
{
    public static class ChannelEndpoints
    {
        public static IEndpointRouteBuilder MapChannelEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/channels")
                           .WithTags("Channels");

            // ✅ ORIGINAL (keep)
            group.MapGet("/list", (IChannelService channelService) =>
            {
                var channels = channelService.GetAll();
                return Results.Ok(channels);
            });

            // ✅ Canonical: /channels (NO ambiguity)
            // IMPORTANT: Keep ONLY ONE of "" or "/"
            // We keep "" and REMOVE "/" to avoid AmbiguousMatchException.
            group.MapGet("", (IChannelService channelService) =>
            {
                var channels = channelService.GetAll();
                return Results.Ok(channels);
            });

            // ✅ ORIGINAL (keep)
            group.MapGet("/info/{slug}", (string slug, IChannelService channelService) =>
            {
                var channel = channelService.GetBySlug(slug);

                if (channel is null)
                    return Results.NotFound(new { message = $"Channel '{slug}' not found" });

                return Results.Ok(channel);
            });

            // ✅ ALIAS (keep)
            group.MapGet("/{slug}", (string slug, IChannelService channelService) =>
            {
                var channel = channelService.GetBySlug(slug);

                if (channel is null)
                    return Results.NotFound(new { message = $"Channel '{slug}' not found" });

                return Results.Ok(channel);
            });

            return app;
        }
    }
}
