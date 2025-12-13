# Firestore Data Model

## 1. Collection: songs

```jsonc
// Collection: songs
{
  "id": "auto-id",          // Firestore document ID

  "name": "string",         // Song title (REQUIRED)
  "singer": "string",       // Artist / Band (REQUIRED)
  "year": 1998,             // Release year (REQUIRED)
  "type": "rap",            // Genre: "rap" | "rnb" | "pop" | "bandari" | ... (REQUIRED)
  "lengthSec": 210,         // Song length in seconds (REQUIRED)

  "mood": ["party"],        // e.g. ["party"], ["blue"], ["motivational"]
  "tags": ["persian"],      // e.g. ["persian", "nostalgia"]

  "audioUrl": "https://...", // MP3 URL in Firebase Storage
  "coverUrl": "https://...", // Cover image URL

  "createdAt": "Timestamp",  // serverTimestamp
  "uploadedBy": "userId",    // uid or "admin",

  "isJingle": false          // optional: true for radio jingles
}
