using Eum.Core.Models;

namespace Eum.Core.Contracts;

public interface IMergedCore
{
    IReadOnlyDictionary<CoreType, IMusicCore> Cores { get; }
    

    Task<IReadOnlyDictionary<CoreType, CoreSearchedResponse>> 
        SearchAsync(string query, CancellationToken ct = default);

    void MergeIds_AndForget(CoreId one, CoreId two);
    IReadOnlyList<IReadOnlyDictionary<CoreType, string>> MergeIds(CoreId one, CoreId two);
}

public enum CoreType
{
    Apple,
    Spotify,
    Local,
}