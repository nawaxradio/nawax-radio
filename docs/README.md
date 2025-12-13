# Nawax Radio – High Level Architecture

## 1. Components

### 1.1. Mobile App (Client)
- Platform: React Native / Flutter (to be decided)
- Responsibilities:
  - UI: Radio / Channels / Settings / Auth
  - Audio playback with crossfade
  - Manage user session
  - Request data from Firebase (Firestore + Storage)

### 1.2. Firebase Layer
- Firebase Authentication
  - Login / Signup
- Cloud Firestore
  - Collections:
    - `songs`
    - `channels`
    - `channelPlaylists` (optional)
    - `users`
- Firebase Storage
  - MP3 files
  - Cover images
- Cloud Functions (optional but recommended)
  - Validate song data on upload
  - Generate daily playlists for channels

### 1.3. Optional: Audio Streaming Server (later)
- For real-time radio-like streaming if needed.
- Not required for MVP (we will start with MP3 + client-side crossfade).
