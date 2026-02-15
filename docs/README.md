Nawax Radio – High Level Architecture (Updated)
1) Components
1.1. Client Apps (Flutter Web + Mobile)

Platform: Flutter (Web + Android/iOS)

Responsibilities:

UI: Home / Channels / Settings

Select channel (Main/Party/Rap/Blue/…)

Audio playback via just_audio

Fetch “now playing” metadata

Handle reconnect / buffering / resume

(Later) caching + prefetch next track

1.2. Backend API (ASP.NET Core Minimal API)

Purpose: Single public entrypoint for the client (instead of direct Firebase access)

Responsibilities:

/channels: list channels + basic info

/radio/{channel}/stream: return a playable stream/URL (24/7 behavior)

/now (or /radio/{channel}/now): return nowPlaying metadata (title, artist, cover, startedAt, etc.)

/health: health check for deployments

/upload (admin only): upload mp3 + metadata (if you keep uploads in API)

Security:

API keys / admin token for upload endpoints

CORS for Flutter Web domains

(Later) server-side rate limit / abuse protection

1.3. Firebase Layer (Storage + Firestore)

Firebase Storage

MP3 files per channel

Cover images (optional)

Cloud Firestore

Collections (recommended current shape):

channels (channel configs, names, order, status)

songs (metadata, storage path, duration, channel, tags)

nowPlaying (current track per channel)

uploads / adminLogs (optional)

Notes:

Client does not need direct access to Firebase for MVP (API handles it).

1.4. Edge / CDN (Cloudflare)

Front-door for:

api.nawaxradio.com

nawaxradio.com (web app)

Responsibilities:

TLS / SSL

Caching static assets (web)

Basic DDoS protection

(Optional, plan-dependent) advanced rate limiting

2) Data & Request Flow
2.1. Channel Discovery

Client → GET /channels

API → Firestore channels

Client renders Channels page and enables instant switch.

2.2. 24/7 Streaming

Client selects channel

Client → GET /radio/{channel}/stream

API → resolves “current playable track” (and/or storage stream) and returns stream response / signed URL

Client plays with just_audio

Client periodically calls /now (or receives updates later via SSE/WebSocket).

2.3. Metadata (Now Playing)

Client → GET /now?channel=main

API → Firestore nowPlaying/{channel}

Client updates UI (song title/artist/cover).

3) Key MVP Decisions (Current)

Flutter is the client (web is already running).

ASP.NET API is the single gateway (stable baseline exists).

No true live transcoded stream yet; instead, continuous playback using track streaming.

Channels are fixed set (no next/prev, only channel selection).

4) Later Enhancements (Roadmap)

Auto-play next track on track end (server picks next + updates nowPlaying)

Caching & prefetch on client

Server-side rate limiting (ASP.NET RateLimiter middleware) if abuse grows

Signed URLs for Storage to reduce direct bucket exposure

Admin panel for uploads + playlist management

Realtime updates (SSE/WebSocket) for nowPlaying

Optional: move to a true radio pipeline (HLS/ICEcast-like) only if needed
