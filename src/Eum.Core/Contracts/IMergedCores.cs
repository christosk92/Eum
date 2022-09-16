using Eum.Core.Contracts.Models;
using Eum.Core.Models;

namespace Eum.Core.Contracts;

public interface IMergedCore
{
    IReadOnlyDictionary<CoreType, IMusicCore> Cores { get; }
    

    Task<IReadOnlyDictionary<CoreType, ICoreResponse>> 
        SearchAsync(string query,
            CoreType[]? cores = null,
            CancellationToken ct = default);
    Task<IReadOnlyDictionary<CoreType, ICoreResponse>> GetArtist(CoreId id,
        CoreType[]? cores = null,
        CancellationToken ct = default);
    void MergeIds_AndForget(CoreId one, CoreId two);
    IReadOnlyList<IReadOnlyDictionary<CoreType, string>> MergeIds(CoreId one, CoreId two);
}

public enum CoreType
{
    Apple,
    Spotify,
    Local,
}