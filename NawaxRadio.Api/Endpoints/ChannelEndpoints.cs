// Endpoints/ChannelEndpoints.cs
using Microsoft.AspNetCore.Routing;
using NawaxRadio.Api.Services;

namespace NawaxRadio.Api.Endpoints
{
    public static class ChannelEndpoints
    {
        public static IEndpointRouteBuilder MapChannelEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/channels")
                           .WithTags("Channels");

            group.MapGet("/list", (IChannelService channelService) =>
            {
                var channels = channelService.GetAll();
                return Results.Ok(channels);
            });

            group.MapGet("/info/{slug}", (string slug, IChannelService channelService) =>
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
