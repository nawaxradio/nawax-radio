// Domain/Song.cs
using System;
using System.Collections.Generic;

namespace NawaxRadio.Api.Domain
{
    public class Song
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = "";
        public string Singer { get; set; } = "";
        public int Year { get; set; }
        public string Type { get; set; } = ""; // genre: rap, rnb, bandari, ...

        public int LengthSec { get; set; }

        public List<string> Mood { get; set; } = new();
        public List<string> Tags { get; set; } = new();

        public string AudioUrl { get; set; } = "";
        public string CoverUrl { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UploadedBy { get; set; } = "admin";

        public bool IsJingle { get; set; } = false;

        public string Language { get; set; } = "fa";

        public bool IsActive { get; set; } = true;

        public int? BitrateKbps { get; set; }
        public long? FileSizeBytes { get; set; }
    }
}
