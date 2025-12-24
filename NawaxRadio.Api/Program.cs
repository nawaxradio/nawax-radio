// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

using IOFile = System.IO.File;
using TagFile = TagLib.File;

using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TagLib;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Logging --------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// =====================================================
// 🔑 FIREBASE / GOOGLE ADC CREDENTIAL
// =====================================================
var credsB64 = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_B64");
if (!string.IsNullOrWhiteSpace(credsB64))
{
    try
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(credsB64));
        var credPath = Path.Combine(Path.GetTempPath(), "firebase-sa.json");
        IOFile.WriteAllText(credPath, json);
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credPath);
    }
    catch
    {
        // ignore - will fail later with a clearer credential error
    }
}

// -------------------- Firebase/Google envs --------------------
var projectId =
    Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID")
    ?? "nawaxradio-b7da4";

if (string.IsNullOrWhiteSpace(projectId))
    throw new Exception("GOOGLE_PROJECT_ID is not set");

// ✅ Use the actual Firebase Storage bucket name (often *.firebasestorage.app)
var bucketName =
    Environment.GetEnvironmentVariable("FIREBASE_BUCKET")
    ?? $"{projectId}.firebasestorage.app";

if (string.IsNullOrWhiteSpace(bucketName))
    bucketName = $"{projectId}.firebasestorage.app";

// -------------------- Services --------------------
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddHttpClient("stream")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        AllowAutoRedirect = true
    });

builder.Services.AddSingleton<SongStore>();
builder.Services.AddSingleton<NowPlayingCache>();

// Firestore
builder.Services.AddSingleton(_ => FirestoreDb.Create(projectId));

// Storage Client (GCS)
builder.Services.AddSingleton(_ => StorageClient.Create());

var app = builder.Build();

// -------------------- Trailing slash normalization --------------------
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value;
    if (!string.IsNullOrWhiteSpace(path) && path.Length > 1 && path.EndsWith("/"))
    {
        var newPath = path.TrimEnd('/');
        ctx.Response.Redirect(newPath + ctx.Request.QueryString, permanent: false);
        return;
    }
    await next();
});

// -------------------- Simple CORS (dev) --------------------
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,HEAD,OPTIONS";
    ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Range, Accept, Origin, Authorization";
    ctx.Response.Headers["Access-Control-Expose-Headers"] = "Content-Length, Content-Range, Accept-Ranges, Content-Type";

    if (ctx.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = 204;
        return;
    }

    await next();
});

// -------------------- Root / Health --------------------
app.MapGet("/", () => Results.Text("NawaxRadio API is running ✅", "text/plain; charset=utf-8"));
app.MapGet("/health", () => Results.Json(new { ok = true, utc = DateTime.UtcNow, projectId, bucketName }));
app.MapGet("/debug/env", () =>
{
    var pid = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
    var fb = Environment.GetEnvironmentVariable("FIREBASE_BUCKET");
    var ga = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    return Results.Json(new { pid, fb, ga });
});

// -------------------- Channels --------------------
var channels = new[]
{
    new ChannelDto("main", "رادیوی اصلی", "ترکیب هیت‌ها ۲۴/۷", "📻"),
    new ChannelDto("party", "پارتی", "انرژی بالا • مهمونی • کلاب", "🎉"),
    new ChannelDto("rap", "رپ", "رپ و هیپ‌هاپ", "🎤"),
    new ChannelDto("shooti", "شوتی", "ریتم‌های شوتی", "🚗"),
    new ChannelDto("blue", "بلو", "آروم و احساسی", "💙"),
    new ChannelDto("motivational", "انگیزشی", "انرژی و انگیزشی", "🔥"),
    new ChannelDto("latest", "جدیدترین", "جدیدترین‌ها", "🆕"),
};

app.MapGet("/channels", () => Results.Json(channels));

// -------------------- Songs --------------------
// ✅ Supports ?channel=main
app.MapGet("/songs", (HttpRequest req, SongStore store) =>
{
    var channel = NormalizeChannel(req.Query["channel"].ToString());
    var list = store.GetAllActive(channel);
    return Results.Json(list);
});

app.MapGet("/songs/{id}", (string id, SongStore store) =>
{
    var s = store.GetById(id);
    return s is null ? Results.NotFound() : Results.Json(s);
});

// -------------------- Radio: now --------------------
app.MapGet("/radio/{channel}/now", (string channel, SongStore store, NowPlayingCache cache) =>
{
    var key = NormalizeChannel(channel);
    if (!channels.Any(c => c.key == key))
        return Results.NotFound(new { error = "Unknown channel" });

    var now = cache.GetOrPick(key, store);
    if (now is null)
        return Results.NotFound(new { error = "No active songs" });

    return Results.Json(new { channel = key, nowPlaying = now });
});

// -------------------- Radio: stream (GET/HEAD + Range proxy) --------------------
app.MapMethods("/radio/{channel}/stream", new[] { "GET", "HEAD" },
    async (HttpContext ctx, string channel, SongStore store, NowPlayingCache cache, IHttpClientFactory httpClientFactory) =>
{
    var key = NormalizeChannel(channel);
    if (!channels.Any(c => c.key == key))
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsJsonAsync(new { error = "Unknown channel" });
        return;
    }

    var now = cache.GetOrPick(key, store);
    if (now is null)
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsJsonAsync(new { error = "No active songs" });
        return;
    }

    // ✅ REPLACE THIS PART (as you asked)
    string signedOrDirectUrl;
    try
    {
        if (!string.IsNullOrWhiteSpace(now.audioUrl) && now.audioUrl.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
        {
            signedOrDirectUrl = CreateSignedUrlFromGsUri(now.audioUrl, TimeSpan.FromHours(2));
        }
        else if (!string.IsNullOrWhiteSpace(now.audioUrl) && now.audioUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // public url هست ولی private ـه => باید تبدیلش کنیم به gs:// و بعد Signed بسازیم
            signedOrDirectUrl = CreateSignedUrlFromPublicGcsUrl(now.audioUrl, TimeSpan.FromHours(2));
        }
        else
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsJsonAsync(new { error = "Song has no usable audioUrl" });
            return;
        }
    }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "SignedUrl failed", detail = ex.Message });
        return;
    }

    var client = httpClientFactory.CreateClient("stream");

    var method = ctx.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
        ? HttpMethod.Head
        : HttpMethod.Get;

    // ✅ Use signedOrDirectUrl instead of signedUrl
    using var req = new HttpRequestMessage(method, signedOrDirectUrl);

    // Forward Range header
    var rangeHeader = ctx.Request.Headers["Range"].ToString();
    if (!string.IsNullOrWhiteSpace(rangeHeader) && RangeHeaderValue.TryParse(rangeHeader, out var parsedRange))
        req.Headers.Range = parsedRange;

    HttpResponseMessage upstream;
    try
    {
        upstream = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
    }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = 502;
        await ctx.Response.WriteAsJsonAsync(new { error = "Upstream stream failed", detail = ex.Message });
        return;
    }

    using (upstream)
    {
        ctx.Response.StatusCode = (int)upstream.StatusCode;
        ctx.Response.Headers["Accept-Ranges"] = "bytes";

        if (upstream.Content.Headers.ContentType is not null)
            ctx.Response.ContentType = upstream.Content.Headers.ContentType.ToString();
        else
            ctx.Response.ContentType = "audio/mpeg";

        if (upstream.Content.Headers.ContentLength.HasValue)
            ctx.Response.ContentLength = upstream.Content.Headers.ContentLength.Value;

        if (upstream.Content.Headers.ContentRange is not null)
            ctx.Response.Headers["Content-Range"] = upstream.Content.Headers.ContentRange.ToString();

        if (ctx.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            return;

        await using var upstreamStream = await upstream.Content.ReadAsStreamAsync(ctx.RequestAborted);
        await upstreamStream.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
    }
});

// -------------------- Admin Upload UI --------------------
app.MapGet("/admin/upload", () =>
{
    var html = """
<!doctype html>
<html lang="fa" dir="rtl">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>Nawax Radio — Admin Upload</title>
  <style>
    :root{
      --bg:#0b0b0b; --card:#111; --text:#fff; --muted:#bdbdbd;
      --brand:#ff481f; --border:#222; --ok:#22c55e; --bad:#ef4444; --warn:#f59e0b; --radius:16px;
    }
    *{box-sizing:border-box}
    body{margin:0;font-family:system-ui,Segoe UI,Tahoma;background:var(--bg);color:var(--text)}
    a{color:inherit;text-decoration:none}
    .wrap{max-width:1100px;margin:0 auto;padding:16px}
    .topbar{display:flex;justify-content:space-between;align-items:center;padding:14px;border:1px solid var(--border);border-radius:var(--radius);margin-bottom:16px;background:#111}
    .nav a{margin-left:8px;padding:8px 12px;border:1px solid var(--border);border-radius:10px;color:var(--muted)}
    .nav a.active{color:#fff;border-color:var(--brand)}
    .grid{display:grid;grid-template-columns:1.2fr .8fr;gap:16px}
    .card{border:1px solid var(--border);border-radius:var(--radius);background:#111}
    .hd{padding:14px;border-bottom:1px solid var(--border)}
    .body{padding:14px}
    label{display:block;font-size:13px;color:var(--muted);margin-bottom:6px}
    input,select,textarea{width:100%;padding:10px;border-radius:10px;border:1px solid #333;background:#0b0b0b;color:#fff}
    textarea{min-height:80px}
    .row{display:grid;grid-template-columns:1fr 1fr;gap:12px}
    .actions{margin-top:12px;display:flex;gap:8px;flex-wrap:wrap}
    .btn{padding:10px 14px;border-radius:10px;border:1px solid #333;background:#151515;color:#fff;cursor:pointer;display:inline-block}
    .btn.primary{background:var(--brand);border-color:var(--brand);color:#000;font-weight:700}
    .status{margin-top:12px;padding:10px;border-radius:10px;border:1px solid var(--border);white-space:pre-wrap;font-size:13px}
    .ok{border-color:var(--ok);color:#d1fae5}
    .bad{border-color:var(--bad);color:#fee2e2}
    .warn{border-color:var(--warn);color:#ffedd5}
  </style>
</head>
<body>
<div class="wrap">
  <div class="topbar">
    <div>📻 <b>Nawax Radio</b> — Admin Upload</div>
    <div class="nav">
      <a href="/" target="_blank">API</a>
      <a href="/channels" target="_blank">Channels</a>
      <a href="/debug/endpoints" target="_blank">Endpoints</a>
      <a class="active" href="/admin/upload">Upload</a>
    </div>
  </div>

  <div class="grid">
    <div class="card">
      <div class="hd"><b>آپلود آهنگ</b></div>
      <div class="body">
        <form id="uploadForm" enctype="multipart/form-data">
          <div class="row">
            <div>
              <label>فایل MP3</label>
              <input type="file" name="file" accept=".mp3,audio/mpeg" required>
            </div>
            <div>
              <label>کانال</label>
              <select id="channel" name="channel" required>
                <option value="">در حال بارگذاری…</option>
              </select>
            </div>
          </div>

          <div class="row" style="margin-top:10px">
            <div>
              <label>نام آهنگ</label>
              <input name="name">
            </div>
            <div>
              <label>خواننده</label>
              <input name="singer">
            </div>
          </div>

          <div class="actions" style="margin-top:12px">
            <button class="btn primary" type="submit">⬆️ آپلود</button>
            <button class="btn" type="button" id="reload">🔄 کانال‌ها</button>
          </div>

          <div id="status" class="status">آماده…</div>
        </form>
      </div>
    </div>

    <div class="card">
      <div class="hd"><b>ابزار سریع</b></div>
      <div class="body">
        <a class="btn" href="/radio/main/now" target="_blank">Now Playing</a>
        <a class="btn" href="/radio/main/stream" target="_blank">Stream</a>
        <a class="btn" href="/songs" target="_blank">Songs</a>
      </div>
    </div>
  </div>
</div>

<script>
const statusEl = document.getElementById('status');
const form = document.getElementById('uploadForm');
const channelSelect = document.getElementById('channel');

function setStatus(t,c){
  statusEl.className = 'status ' + (c || '');
  statusEl.textContent = t;
}

async function loadChannels(){
  setStatus('در حال دریافت کانال‌ها…','warn');
  try{
    const r = await fetch('/channels', { cache:'no-store' });
    if(!r.ok) throw new Error('HTTP '+r.status);
    const ch = await r.json();

    channelSelect.innerHTML = '<option value="">انتخاب کانال</option>';
    ch.forEach(c=>{
      const o=document.createElement('option');
      o.value=c.key;
      o.textContent=(c.emoji||'')+' '+c.title;
      channelSelect.appendChild(o);
    });

    setStatus('کانال‌ها آماده‌اند ✅','ok');
  }catch(e){
    setStatus('خطا در دریافت کانال‌ها ❌','bad');
  }
}

form.addEventListener('submit', async e=>{
  e.preventDefault();
  const fd = new FormData(form);
  setStatus('در حال آپلود…','warn');

  try{
    const r = await fetch('/admin/upload/song', { method:'POST', body: fd });
    const txt = await r.text();
    if(!r.ok) throw txt;

    let data;
    try{ data = JSON.parse(txt); }catch{ data = txt; }
    setStatus('آپلود موفق ✅\\n'+JSON.stringify(data,null,2),'ok');
  }catch(err){
    setStatus('آپلود ناموفق ❌\\n'+err,'bad');
  }
});

document.getElementById('reload').onclick = loadChannels;
loadChannels();
</script>
</body>
</html>
""";
    return Results.Text(html, "text/html; charset=utf-8");
});

// -------------------- Admin Upload API --------------------
app.MapPost("/admin/upload/song", async (
    HttpContext ctx,
    SongStore store,
    FirestoreDb firestore,
    StorageClient storage
) =>
{
    if (!ctx.Request.HasFormContentType)
        return Results.BadRequest(new { error = "multipart/form-data required" });

    var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "file is required" });

    var channel = NormalizeChannel(form["channel"].ToString());
    if (string.IsNullOrWhiteSpace(channel)) channel = "main";

    var id = Guid.NewGuid().ToString("N");

    var tempPath = Path.GetTempFileName();
    await using (var fs = System.IO.File.Create(tempPath))
        await file.CopyToAsync(fs, ctx.RequestAborted);

    string name = (form["name"].ToString() ?? "").Trim();
    string singer = (form["singer"].ToString() ?? "").Trim();

    var yearStr = (form["year"].ToString() ?? "").Trim();
    var year = DateTime.UtcNow.Year;
    if (int.TryParse(yearStr, out var y) && y >= 1900 && y <= 2100) year = y;

    int lengthSec = 0;

    try
    {
        var tf = TagFile.Create(tempPath);
        if (string.IsNullOrWhiteSpace(name)) name = tf.Tag.Title ?? "";
        if (string.IsNullOrWhiteSpace(singer)) singer = tf.Tag.FirstPerformer ?? "";
        if (tf.Tag.Year > 0) year = (int)tf.Tag.Year;
        lengthSec = (int)tf.Properties.Duration.TotalSeconds;
    }
    catch { }

    if (string.IsNullOrWhiteSpace(name))
        name = Path.GetFileNameWithoutExtension(file.FileName) ?? "Unknown Title";

    if (string.IsNullOrWhiteSpace(singer))
        singer = "Unknown";

    var objectName = $"songs/{channel}/{id}.mp3";

    var bucket = (Environment.GetEnvironmentVariable("FIREBASE_BUCKET") ?? bucketName ?? "").Trim();
    if (bucket.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
        bucket = bucket.Substring("gs://".Length);

    bucket = bucket.Replace("https://storage.googleapis.com/", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("http://storage.googleapis.com/", "", StringComparison.OrdinalIgnoreCase)
                   .Trim()
                   .TrimEnd('/');

    Console.WriteLine($"[UPLOAD] bucket='{bucket}' object='{objectName}'");

    await using (var uploadStream = System.IO.File.OpenRead(tempPath))
    {
        await storage.UploadObjectAsync(
            bucket: bucket,
            objectName: objectName,
            contentType: "audio/mpeg",
            source: uploadStream,
            cancellationToken: ctx.RequestAborted
        );
    }

    try { System.IO.File.Delete(tempPath); } catch { }

    // ✅ IMPORTANT: store gs://... (not public url)
    var gsUri = $"gs://{bucket}/{objectName}";

    var song = new SongDto(
        id: id,
        name: name,
        singer: singer,
        year: year,
        type: "unknown",
        lengthSec: lengthSec,
        audioUrl: gsUri,
        isActive: true,
        channel: channel
    );

    await firestore.Collection("songs").Document(id).SetAsync(new
    {
        song.id,
        song.name,
        song.singer,
        song.year,
        song.type,
        song.lengthSec,
        song.audioUrl,
        song.isActive,
        song.channel,
        createdAt = Timestamp.GetCurrentTimestamp()
    }, cancellationToken: ctx.RequestAborted);

    store.Upsert(song);

    return Results.Json(new { ok = true, song, bucketName = bucket, objectName });
});

// -------------------- Debug endpoints list --------------------
app.MapGet("/debug/endpoints", (IEnumerable<EndpointDataSource> sources) =>
{
    var list = sources
        .SelectMany(s => s.Endpoints)
        .OfType<RouteEndpoint>()
        .Select(e => new
        {
            route = e.RoutePattern.RawText,
            methods = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? Array.Empty<string>()
        })
        .OrderBy(x => x.route)
        .ToList();

    return Results.Json(list);
});

// -------------------- Load songs from Firestore into RAM (startup) --------------------
{
    try
    {
        using var scope = app.Services.CreateScope();
        var firestore = scope.ServiceProvider.GetRequiredService<FirestoreDb>();
        var store = scope.ServiceProvider.GetRequiredService<SongStore>();

        var snap = await firestore.Collection("songs")
            .WhereEqualTo("isActive", true)
            .GetSnapshotAsync();

        var loaded = 0;

        foreach (var doc in snap.Documents)
        {
            var d = doc.ToDictionary();

            string GetStr(string k) => d.TryGetValue(k, out var v) ? (v?.ToString() ?? "") : "";
            int GetInt(string k, int def = 0)
            {
                if (!d.TryGetValue(k, out var v) || v is null) return def;
                if (v is long l) return (int)l;
                if (v is int i) return i;
                if (int.TryParse(v.ToString(), out var p)) return p;
                return def;
            }
            bool GetBool(string k, bool def = false)
            {
                if (!d.TryGetValue(k, out var v) || v is null) return def;
                if (v is bool b) return b;
                if (bool.TryParse(v.ToString(), out var p)) return p;
                return def;
            }

            var song = new SongDto(
                id: GetStr("id"),
                name: GetStr("name"),
                singer: GetStr("singer"),
                year: GetInt("year", DateTime.UtcNow.Year),
                type: GetStr("type"),
                lengthSec: GetInt("lengthSec", 0),
                audioUrl: GetStr("audioUrl"),
                isActive: GetBool("isActive", true),
                channel: NormalizeChannel(GetStr("channel"))
            );

            if (!string.IsNullOrWhiteSpace(song.id))
            {
                store.Upsert(song);
                loaded++;
            }
        }

        Console.WriteLine($"[BOOT] Loaded {loaded} active songs from Firestore.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[BOOT] Firestore load failed: {ex.Message}");
    }
}

// -------------------- Seed (only if still empty) --------------------
{
    var store = app.Services.GetRequiredService<SongStore>();
    if (!store.GetAllActive().Any())
    {
        store.Upsert(new SongDto(
            id: "seed-1",
            name: "Sample (upload your first song)",
            singer: "Nawax",
            year: DateTime.UtcNow.Year,
            type: "unknown",
            lengthSec: 0,
            audioUrl: "",
            isActive: true,
            channel: "main"
        ));
    }
}

app.Run();

// ========================= Helpers / DTOs / Stores =========================

static string NormalizeChannel(string? c)
{
    return (c ?? "").Trim().ToLowerInvariant();
}

static string CreateSignedUrlFromGsUri(string gsUri, TimeSpan ttl)
{
    var s = gsUri.Trim();
    if (!s.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
        throw new Exception("Invalid gsUri (expected gs://bucket/object)");

    s = s.Substring("gs://".Length);
    var slash = s.IndexOf('/');
    if (slash <= 0 || slash >= s.Length - 1)
        throw new Exception("Invalid gsUri (missing objectName)");

    var bucket = s.Substring(0, slash);
    var objectName = s.Substring(slash + 1);

    return CreateSignedUrl(bucket, objectName, ttl);
}

// ✅ NEW helper you asked to add
static string CreateSignedUrlFromPublicGcsUrl(string url, TimeSpan ttl)
{
    // sample:
    // https://storage.googleapis.com/<bucket>/<object>
    // https://storage.googleapis.com/nawaxradio-b7da4.firebasestorage.app/songs/blue/xxx.mp3

    var u = new Uri(url);
    var path = u.AbsolutePath.TrimStart('/'); // bucket/object...
    var firstSlash = path.IndexOf('/');
    if (firstSlash <= 0 || firstSlash >= path.Length - 1)
        throw new Exception("Invalid GCS public URL");

    var bucket = path.Substring(0, firstSlash);
    var objectName = path.Substring(firstSlash + 1);

    return CreateSignedUrl(bucket, objectName, ttl);
}

// ✅ Signed URL generator (NO red / no expiresAt overload)
static string CreateSignedUrl(string bucket, string objectName, TimeSpan ttl)
{
    // Uses ADC (GOOGLE_APPLICATION_CREDENTIALS)
    var cred = GoogleCredential.GetApplicationDefault();

    // UrlSigner from current library API
    var signer = UrlSigner.FromCredential(cred);

    var options = UrlSigner.Options.FromDuration(ttl);

    var template = UrlSigner.RequestTemplate
        .FromBucket(bucket)
        .WithObjectName(objectName)
        .WithHttpMethod(HttpMethod.Get);

    return signer.Sign(template, options);
}

record ChannelDto(string key, string title, string subtitle, string emoji);

record SongDto(
    string id,
    string name,
    string singer,
    int year,
    string type,
    int lengthSec,
    string audioUrl,   // can be gs://... OR old https://...
    bool isActive,
    string channel
);

sealed class SongStore
{
    private readonly ConcurrentDictionary<string, SongDto> _songs = new();

    public IEnumerable<SongDto> GetAllActive(string? channel = null)
    {
        var q = _songs.Values.Where(s => s.isActive);

        if (!string.IsNullOrWhiteSpace(channel))
            q = q.Where(s => s.channel == channel);

        return q.OrderByDescending(s => s.year).ToList();
    }

    public SongDto? GetById(string id)
        => _songs.TryGetValue(id, out var s) ? s : null;

    public void Upsert(SongDto song)
        => _songs[song.id] = song;

    public SongDto? PickForChannel(string channel)
    {
        // only pick songs that have usable audioUrl
        var list = _songs.Values
            .Where(s => s.isActive && s.channel == channel && !string.IsNullOrWhiteSpace(s.audioUrl))
            .ToList();

        if (list.Count == 0)
            list = _songs.Values.Where(s => s.isActive && !string.IsNullOrWhiteSpace(s.audioUrl)).ToList();

        if (list.Count == 0) return null;

        return list[Random.Shared.Next(list.Count)];
    }
}

sealed class NowPlayingCache
{
    private sealed class Entry
    {
        public SongDto Song { get; init; } = default!;
        public DateTimeOffset ExpiresAt { get; init; }
    }

    private readonly ConcurrentDictionary<string, Entry> _cache = new();

    public SongDto? GetOrPick(string channel, SongStore store)
    {
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(channel, out var e) && e.ExpiresAt > now)
            return e.Song;

        var picked = store.PickForChannel(channel);
        if (picked is null) return null;

        var ttlSec = picked.lengthSec > 10 ? picked.lengthSec : 120;

        _cache[channel] = new Entry
        {
            Song = picked,
            ExpiresAt = now.AddSeconds(ttlSec)
        };

        return picked;
    }
}
