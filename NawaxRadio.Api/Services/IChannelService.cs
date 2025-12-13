using System.Collections.Generic;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services;

public interface IChannelService
{
    IEnumerable<Channel> GetAll();
    Channel? GetBySlug(string slug);
    Channel? GetById(string id);
}
