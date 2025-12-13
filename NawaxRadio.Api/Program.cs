// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using NawaxRadio.Api.Options;
using NawaxRadio.Api.Services;
using NawaxRadio.Api.Endpoints;
using NawaxRadio.Api.Domain;
using TagLib;

// ==============================
// 📻 CHANNELS (STATIC CONFIG)
// ==============================
var channels = new List<Channel>
{
    new()
    {
        Id = "1",
        Title = "Main Radio",
        Name = "Main Radio",
        Key = "main",
        Description = "ترکیب هیت‌های فارسی برای همه حال‌و‌هواها",
        Emoji = "📻",
        SortOrder = 1,
        Filter = new ChannelFilter(),
        PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
    },
    new()
    {
        Id = "2",
        Title = "Ghery",
        Name = "Ghery",
        Key = "ghery",
        Description = "آهنگ‌های عشقولانه و گریه‌ای",
        Emoji = "💔",
        SortOrder = 2,
        Filter = new ChannelFilter
        {
            Mood = new() { "ghery", "blue", "dep" }
        }
    },
    new()
    {
        Id = "3",
        Title = "Party",
        Name = "Party",
        Key = "party",
        Description = "آهنگ‌های شاد و انرژی‌دار",
        Emoji = "🎉",
        SortOrder = 3,
        Filter = new ChannelFilter
        {
            Mood = new() { "party" }
        }
    },
    new()
    {
        Id = "4",
        Title = "Gen Z",
        Name = "Gen Z",
        Key = "genz",
        Description = "ترک‌های مدرن نسل Z",
        Emoji = "🧬",
        SortOrder = 4,
        Filter = new ChannelFilter
        {
            Mood = new() { "genz" },
            Type = new() { "trap", "modern" }
        }
    },
    new()
    {
        Id = "5",
        Title = "Rap / HipHop",
        Name = "Rap / HipHop",
        Key = "rap",
        Description = "رپ و هیپ‌هاپ فارسی",
        Emoji = "🎤",
        SortOrder = 5,
        Filter = new ChannelFilter
        {
            Type = new() { "rap", "hiphop" }
        }
    },
    new()
    {
        Id = "6",
        Title = "Bandari",
        Name = "Bandari",
        Key = "bandari",
        Description = "جنوبی و بندری",
        Emoji = "🌊",
        SortOrder = 6,
        Filter = new ChannelFilter
        {
            Type = new() { "bandari", "jonobi" }
        }
    },
    new()
    {
        Id = "7",
        Title = "Dep Mood",
        Name = "Dep Mood",
        Key = "dep",
        Description = "مود آبی و دپ",
        Emoji = "💙",
        SortOrder = 7,
        Filter = new ChannelFilter
        {
            Mood = new() { "dep", "blue" }
        }
    },
    new()
    {
        Id = "8",
        Title = "Energy",
        Name = "Energy",
        Key = "energy",
        Description = "انرژی و ورزش",
        Emoji = "⚡",
        SortOrder = 8,
        Filter = new ChannelFilter
        {
            Mood = new() { "energy" }
        }
    },
    new()
    {
        Id = "9",
        Title = "Latest Hits",
        Name = "Latest Hits",
        Key = "latest",
        Description = "جدیدترین آهنگ‌ها",
        Emoji = "🆕",
        SortOrder = 9,
        Filter = new ChannelFilter
        {
            Latest = true
        }
    }
};

// share channels globally
ChannelStore.Channels = channels;

// ==============================
// BUILDER
// ==============================
var builder = WebApplication.CreateBuilder(args);

// Firebase
builder.Services.Configure<FirebaseStorageOptions>(
    builder.Configuration.GetSection("Firebase"));

// Core services
builder.Services.AddSingleton<ICloudStorage, FirebaseStorageService>();
builder.Services.AddSingleton<IFirestoreSongRepository, FirestoreSongRepository>();
builder.Services.AddSingleton<ISongService, InMemorySongService>();
builder.Services.AddSingleton<IChannelService, InMemoryChannelService>();

// 🔥 Startup sync Firestore → InMemory
builder.Services.AddHostedService<StartupSongSyncService>();

// Antiforgery + CORS
builder.Services.AddAntiforgery();
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFlutter", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ==============================
// MIDDLEWARE
// ==============================
app.UseCors("AllowFlutter");
app.UseAntiforgery();

// ==============================
// ROOT + HEALTH
// ==============================
app.MapGet("/", () => "Nawax Radio API is running...");

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "OK",
        timeUtc = DateTime.UtcNow
    });
});

// ==============================
// DEBUG ENDPOINTS
// ==============================
app.MapGet("/debug/endpoints", (EndpointDataSource source) =>
{
    var list = source.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => new
        {
            pattern = e.RoutePattern.RawText,
            methods = string.Join(",",
                e.Metadata.OfType<HttpMethodMetadata>()
                 .FirstOrDefault()?.HttpMethods ?? Array.Empty<string>())
        })
        .OrderBy(e => e.pattern);

    return Results.Ok(list);
});

// ==============================
// CHANNELS
// ==============================
app.MapGet("/channels", () =>
{
    return Results.Ok(
        channels.OrderBy(c => c.SortOrder).Select(c => new
        {
            slug = c.Key,
            name = c.Name,
            description = c.Description,
            emoji = c.Emoji
        }));
});

// ==============================
// ENDPOINT MODULES
// ==============================
app.MapUploadEndpoints();
app.MapSongEndpoints();
app.MapChannelEndpoints();
app.MapRadioEndpoints();

// ==============================
app.Run();
