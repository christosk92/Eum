using Eum.Core.Contracts.Models;

namespace Eum.Albums
{
    public interface IIndexedTrack : ITrack
    {
        int Index { get; }
    }
}
