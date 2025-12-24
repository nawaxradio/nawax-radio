using System;
using System.Collections.Generic;
using System.Linq;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services;

public class InMemoryChannelService : IChannelService
{
    // ✅ Never keep a reference to a mutable static list.
    // ✅ Always read the latest snapshot (so edits to ChannelStore reflect immediately after restart).
    private static List<Channel> Snapshot()
    {
        // ChannelStore.Channels might be null or might be the old list instance.
        // We clone to avoid accidental runtime mutations and to ensure stable ordering.
        var src = ChannelStore.Channels ?? new List<Channel>();
        return src.Where(c => c != null).ToList();
    }

    public IEnumerable<Channel> GetAll()
    {
        var list = Snapshot();

        // Stable + safe ordering:
        // 1) SortOrder (if exists)
        // 2) Title/Name
        // 3) Key
        return list
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => (c.Title ?? c.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => (c.Key ?? string.Empty), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Channel? GetBySlug(string slug) => GetByKey(slug);

    public Channel? GetByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var k = key.Trim();

        var list = Snapshot();

        // 1) match by Key (slug)
        var byKey = list.FirstOrDefault(c =>
            string.Equals((c.Key ?? string.Empty).Trim(), k, StringComparison.OrdinalIgnoreCase));

        if (byKey != null) return byKey;

        // 2) match by Title/Name
        var byName = list.FirstOrDefault(c =>
            string.Equals((c.Title ?? string.Empty).Trim(), k, StringComparison.OrdinalIgnoreCase) ||
            string.Equals((c.Name ?? string.Empty).Trim(), k, StringComparison.OrdinalIgnoreCase));

        if (byName != null) return byName;

        // 3) tolerant match: slug-like (spaces -> -, lower)
        static string Slugify(string s) =>
            (s ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '-');

        var kk = Slugify(k);

        return list.FirstOrDefault(c =>
            Slugify(c.Key) == kk ||
            Slugify(c.Title) == kk ||
            Slugify(c.Name) == kk);
    }

    public Channel? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var list = Snapshot();
        var k = id.Trim();

        return list.FirstOrDefault(c =>
            string.Equals((c.Id ?? string.Empty).Trim(), k, StringComparison.OrdinalIgnoreCase));
    }
}
