using System.Collections.Generic;
using NawaxRadio.Api.Domain;

namespace NawaxRadio.Api.Services
{
    public interface ISongService
    {
        IEnumerable<Song> GetAll();

        Song? GetById(string id);

        IEnumerable<Song> GetByChannel(Channel channel);

        Song Add(Song song);

        bool Delete(string id);
    }
}
