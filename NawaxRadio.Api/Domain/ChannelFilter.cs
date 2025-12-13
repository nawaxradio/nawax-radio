using System.Collections.Generic;

namespace NawaxRadio.Api.Domain
{
    public class ChannelFilter
    {
        public List<string> Type { get; set; } = new();
        public List<string> Mood { get; set; } = new();

        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }

        public bool Latest { get; set; } = false;
    }
}
