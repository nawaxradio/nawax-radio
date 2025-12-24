// Endpoints/RadioEndpoints.cs
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NawaxRadio.Api.Services;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Endpoints
{
    public static class RadioEndpoints
    {
        private static readonly ThreadLocal<Random> _rng = new(() => new Random());

        public static IEndpointRouteBuilder MapRadioEndpoints(this IEndpointRouteBuilder app)
        {
            // ✅ JSON "now"
            app.MapGet("/radio/{channelKey}/now", Now);

            // ✅ ONE route for both GET + HEAD (prevents 405 forever)
            app.MapMethods("/radio/{channelKey}/stream", new[] { "GET", "HEAD" }, StreamGetOrHead);

            return app;
        }

        // -----------------------------
        // Shared selection logic
        // -----------------------------
        private static bool TryPickSong(
            string channelKey,
            ISongService songs,
            IChannelService channels,
            out Song? picked,
            out string resolvedChannelKey,
            out object? errorPayload
        )
        {
            picked = null;
            errorPayload = null;

            var key = (channelKey ?? "").Trim().ToLowerInvariant();
            resolvedChannelKey = key;

            if (string.IsNullOrWhiteSpace(key))
            {
                errorPayload = new { error = "invalid_channelKey" };
                return false;
            }

            // ✅ main = mix of all active songs
            if (key == "main")
            {
                var allActive = songs.GetAll().ToList();
                if (allActive.Count == 0)
                {
                    errorPayload = new
                    {
                        error = "no_songs_in_memory",
                        channelKey = "main",
                        inMemoryTotal = 0
                    };
                    return false;
                }

                picked = allActive[_rng.Value!.Next(0, allActive.Count)];
                resolvedChannelKey = "main";
                return true;
            }

            var ch = channels.GetBySlug(key);
            if (ch == null)
            {
                errorPayload = new { error = "channel_not_found", channelKey = key };
                return false;
            }

            resolvedChannelKey = ch.Key ?? key;

            var list = songs.GetByChannel(ch).ToList();
            if (list.Count == 0)
            {
                var all = songs.GetAll().ToList();
                errorPayload = new
                {
                    error = "no_songs_for_channel",
                    channelKey = key,
                    channelResolvedKey = resolvedChannelKey,
                    inMemoryTotal = all.Count,
                    hint = "Check Song.Mood contains channelKey (e.g., 'rap', 'genz', etc.). For main we return all active songs.",
                    sample = all.Take(5)
                };
                return false;
            }

            picked = list[_rng.Value!.Next(0, list.Count)];
            return true;
        }

        private static IResult Now(
            string channelKey,
            ISongService songs,
            IChannelService channels)
        {
            if (!TryPickSong(channelKey, songs, channels, out var s, out var resolvedChannel, out var err))
            {
                var errCode = (err as dynamic)?.error as string;

                if (string.Equals(errCode, "invalid_channelKey", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(err);

                if (string.Equals(errCode, "channel_not_found", StringComparison.OrdinalIgnoreCase))
                    return Results.NotFound(err);

                return Results.Json(err, statusCode: 503);
            }

            var url = CleanUrl(s!.AudioUrl);
            if (string.IsNullOrWhiteSpace(url))
                return Results.Json(new { error = "empty_audioUrl", songId = s.Id }, statusCode: 502);

            return Results.Ok(new
            {
                audioUrl = url,
                songId = s.Id,
                name = s.Name,
                singer = s.Singer,
                channel = resolvedChannel,
                isJingle = s.IsJingle
            });
        }

        private static string CleanUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";

            url = url.Trim().Replace("\r", "").Replace("\n", "");

            // ✅ ensure alt=media exists
            if (!url.Contains("alt=media", StringComparison.OrdinalIgnoreCase))
                url += url.Contains('?') ? "&alt=media" : "?alt=media";

            return url;
        }

        // -----------------------------
        // GET + HEAD unified handler
        // -----------------------------
        private static async Task StreamGetOrHead(
            HttpContext ctx,
            string channelKey,
            ISongService songs,
            IChannelService channels,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory lf,
            CancellationToken ct)
        {
            var logger = lf.CreateLogger("RadioStream");

            ctx.Response.Headers["X-Nawax-StreamMode"] = "proxy";

            if (!TryPickSong(channelKey, songs, channels, out var s, out var resolvedChannel, out var err))
            {
                var errCode = (err as dynamic)?.error as string;

                if (string.Equals(errCode, "invalid_channelKey", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsJsonAsync(err, ct);
                    return;
                }

                if (string.Equals(errCode, "channel_not_found", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsJsonAsync(err, ct);
                    return;
                }

                ctx.Response.StatusCode = 503;
                await ctx.Response.WriteAsJsonAsync(err, ct);
                return;
            }

            var url = CleanUrl(s!.AudioUrl);
            if (string.IsNullOrWhiteSpace(url))
            {
                ctx.Response.StatusCode = 502;
                await ctx.Response.WriteAsJsonAsync(new { error = "empty_audioUrl", songId = s.Id }, ct);
                return;
            }

            // common headers
            ctx.Response.Headers["Accept-Ranges"] = "bytes";
            ctx.Response.Headers["X-Nawax-SongId"] = s.Id ?? "";
            ctx.Response.Headers["X-Nawax-Channel"] = resolvedChannel;

            // ✅ HEAD: no body
            if (HttpMethods.IsHead(ctx.Request.Method))
            {
                try
                {
                    var client = httpClientFactory.CreateClient();

                    // safest: minimal ranged GET (0-0) to get headers even if HEAD unsupported upstream
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.TryAddWithoutValidation("Range", "bytes=0-0");

                    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (resp.StatusCode != HttpStatusCode.OK &&
                        resp.StatusCode != HttpStatusCode.PartialContent)
                    {
                        ctx.Response.StatusCode = 502;
                        await ctx.Response.WriteAsJsonAsync(new
                        {
                            error = "upstream_failed",
                            upstreamStatus = (int)resp.StatusCode,
                            songId = s.Id
                        }, ct);
                        return;
                    }

                    ctx.Response.StatusCode = 200;

                    var contentType = resp.Content.Headers.ContentType?.ToString();
                    ctx.Response.ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType;

                    if (resp.Content.Headers.ContentLength.HasValue)
                        ctx.Response.ContentLength = resp.Content.Headers.ContentLength.Value;

                    if (resp.Content.Headers.ContentRange != null)
                        ctx.Response.Headers["Content-Range"] = resp.Content.Headers.ContentRange.ToString();

                    // end (NO body)
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "HEAD stream failed channel={Channel} songId={SongId}", resolvedChannel, s.Id);
                    if (!ctx.Response.HasStarted)
                    {
                        ctx.Response.StatusCode = 500;
                        await ctx.Response.WriteAsJsonAsync(new { error = "stream_head_failed", message = ex.Message }, ct);
                    }
                    return;
                }
            }

            // ✅ GET: proxy bytes + Range support
            var range = ctx.Request.Headers["Range"].ToString();

            try
            {
                var client = httpClientFactory.CreateClient();

                using var req = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrWhiteSpace(range))
                    req.Headers.TryAddWithoutValidation("Range", range);

                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (resp.StatusCode != HttpStatusCode.OK &&
                    resp.StatusCode != HttpStatusCode.PartialContent)
                {
                    logger.LogWarning("Upstream bad status {Status} songId={SongId}",
                        (int)resp.StatusCode, s.Id);

                    ctx.Response.StatusCode = 502;
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "upstream_failed",
                        upstreamStatus = (int)resp.StatusCode,
                        songId = s.Id
                    }, ct);
                    return;
                }

                ctx.Response.StatusCode = (int)resp.StatusCode;

                var contentType = resp.Content.Headers.ContentType?.ToString();
                ctx.Response.ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType;

                if (resp.Content.Headers.ContentLength.HasValue)
                    ctx.Response.ContentLength = resp.Content.Headers.ContentLength.Value;

                if (resp.Content.Headers.ContentRange != null)
                    ctx.Response.Headers["Content-Range"] = resp.Content.Headers.ContentRange.ToString();

                await using var upstream = await resp.Content.ReadAsStreamAsync(ct);
                await upstream.CopyToAsync(ctx.Response.Body, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stream failed channel={Channel} songId={SongId}", resolvedChannel, s.Id);

                if (!ctx.Response.HasStarted)
                {
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.WriteAsJsonAsync(new { error = "stream_failed", message = ex.Message }, ct);
                }
            }
        }
    }
}
