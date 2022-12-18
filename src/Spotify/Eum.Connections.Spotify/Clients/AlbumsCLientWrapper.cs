using System;
using System.Collections.Generic;
using System.Text;
using Eum.Connections.Spotify.Clients.Contracts;

namespace Eum.Connections.Spotify.Clients
{
    public class AlbumsCLientWrapper : IAlbumsClient
    {
        public AlbumsCLientWrapper(IMercuryAlbumsClient mercury)
        {
            Mercury = mercury;
        }

        public IMercuryAlbumsClient Mercury { get; }
    }
}
