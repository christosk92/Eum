namespace Eum.Core.Contracts;

public interface IUnifiedIdsConfiguration
{
    void MergeId(params  CoreId[] ids);
    IReadOnlyDictionary<CoreType, string> GetIds(CoreId main);
}

public record CoreId 
{
    public CoreType Type { get; init; }
    public string Id { get; init; }
}