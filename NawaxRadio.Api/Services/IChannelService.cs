using System.Collections.Generic;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services;

public interface IChannelService
{
    IEnumerable<Channel> GetAll();

    // existing
    Channel? GetBySlug(string slug);

    // ✅ add this (RadioEndpoints uses it)
    Channel? GetByKey(string key);

    Channel? GetById(string id);
}
