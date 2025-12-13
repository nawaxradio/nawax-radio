using System.Collections.Generic;
using System.Linq;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services;

public class InMemoryChannelService : IChannelService
{
    private readonly List<Channel> _channels;

    public InMemoryChannelService()
    {
        _channels = ChannelStore.Channels;
    }

    public IEnumerable<Channel> GetAll()
    {
        return _channels.OrderBy(c => c.SortOrder);
    }

    public Channel? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return _channels.FirstOrDefault(c =>
            c.Key.Equals(slug, System.StringComparison.OrdinalIgnoreCase));
    }

    public Channel? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return _channels.FirstOrDefault(c => c.Id == id);
    }
}
