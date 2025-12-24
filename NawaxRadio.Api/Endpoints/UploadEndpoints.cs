// Endpoints/UploadEndpoints.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NawaxRadio.Api.Domain;
using NawaxRadio.Api.Services;
using TagLib;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace NawaxRadio.Api.Endpoints
{
    public static class UploadEndpoints
    {
        public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
        {
            var uploadGroup = app.MapGroup("/upload").DisableAntiforgery();

            uploadGroup.MapPost("/debug", (HttpRequest request) =>
            {
                return Results.Ok(new
                {
                    hasFormContentType = request.HasFormContentType,
                    contentType = request.ContentType,
                    method = request.Method,
                    path = request.Path.ToString()
                });
            });

            uploadGroup.MapPost("/song", async (
                [FromForm] IFormFile file,
                [FromForm] string? name,
                [FromForm] string? singer,
                [FromForm] int? year,
                [FromForm] string? type,
                [FromForm] string? mood,
                [FromForm] string? tags,
                [FromForm] bool? isJingle,
                [FromForm] string? language,
                [FromServices] ISongService songService,
                [FromServices] ICloudStorage cloudStorage,
                [FromServices] IFirestoreSongRepository firestoreSongRepository) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest("No file uploaded (field name must be 'file').");
                }

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext))
                {
                    ext = ".mp3";
                }

                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid()}{ext}"
                );

                try
                {
                    await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await file.CopyToAsync(fs);
                    }

                    using var tagFile = TagLib.File.Create(tempPath);
                    var tag = tagFile.Tag;
                    var props = tagFile.Properties;

                    var detectedName = string.IsNullOrWhiteSpace(tag.Title)
                        ? Path.GetFileNameWithoutExtension(file.FileName)
                        : tag.Title;

                    var detectedSinger = tag.Performers?.FirstOrDefault()
                                         ?? tag.FirstPerformer
                                         ?? "Unknown";

                    var detectedYear = tag.Year == 0 ? DateTime.UtcNow.Year : (int)tag.Year;

                    var lengthSec = (int)props.Duration.TotalSeconds;

                    var finalName = !string.IsNullOrWhiteSpace(name) ? name! : detectedName;
                    var finalSinger = !string.IsNullOrWhiteSpace(singer) ? singer! : detectedSinger;
                    var finalYear = year.HasValue && year.Value > 0 ? year.Value : detectedYear;
                    var finalType = !string.IsNullOrWhiteSpace(type) ? type! : "unknown";
                    var finalLanguage = !string.IsNullOrWhiteSpace(language) ? language! : "fa";
                    var finalIsJingle = isJingle ?? false;

                    var moodList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(mood))
                    {
                        moodList = mood!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(m => m.Trim())
                            .Where(m => m.Length > 0)
                            .ToList();
                    }

                    var tagsList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(tags))
                    {
                        tagsList = tags!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => t.Length > 0)
                            .ToList();
                    }

                    string audioUrl;
                    await using (var uploadStream = System.IO.File.OpenRead(tempPath))
                    {
                        audioUrl = await cloudStorage.UploadSongAsync(
                            uploadStream,
                            file.FileName,
                            file.ContentType ?? "audio/mpeg");
                    }

                    var song = new Song
                    {
                        Name = finalName,
                        Singer = finalSinger,
                        Year = finalYear,
                        Type = finalType,
                        LengthSec = lengthSec,
                        Mood = moodList,
                        Tags = tagsList,
                        AudioUrl = audioUrl,
                        CoverUrl = "",
                        Language = finalLanguage,
                        IsJingle = finalIsJingle,
                        UploadedBy = "admin",
                        FileSizeBytes = file.Length,
                        IsActive = true
                    };

                    var created = songService.Add(song);

                    // ✅ keep this (firestore sync)
                    await firestoreSongRepository.SaveAsync(created);

                    return Results.Ok(created);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to read or upload song: {ex.Message}");
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            });

            // ✅ IMPORTANT:
            // Do NOT map /songs endpoints here (causes AmbiguousMatch).
            // All /songs routes live in SongEndpoints.cs only.

            return app;
        }
    }
}
