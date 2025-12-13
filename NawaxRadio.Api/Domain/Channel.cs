using System.Collections.Generic;

namespace NawaxRadio.Api.Domain
{
    public class Channel
    {
        // -----------------------------
        // Base / UI
        // -----------------------------
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        // -----------------------------
        // Radio Engine
        // -----------------------------
        public string Key { get; set; } = string.Empty;     // slug
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;

        public ChannelFilter Filter { get; set; } = new ChannelFilter();
        public PlaylistConfig PlaylistConfig { get; set; } = new PlaylistConfig();
    }
}
