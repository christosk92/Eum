using System.Collections.Concurrent;
using Eum.Core.Contracts;

namespace Eum.Core;

public sealed class GenericUnifiedIdsConfiguration : IUnifiedIdsConfiguration
{
    private readonly ConcurrentDictionary<CoreId, HashSet<CoreId>> _map = new();

    public void MergeId(params CoreId[] ids)
    {
        foreach (var coreId in ids)
        {
            var neq = ids.Where(a => !a.Equals(coreId)).ToArray();
            _map.AddOrUpdate(coreId, id =>
                new HashSet<CoreId>(neq), (id, ids) =>
            {
                foreach (var to in neq)
                {
                   ids.Add(to);
                }
                return ids;
            });

        }
    }

    public IReadOnlyDictionary<CoreType ,string> GetIds(CoreId main)
    {
        var findAllIds =
            _map.Where(a => a
                                .Key.Equals(main)
                            || a.Value
                                .Any(k => k.Equals(main)));
        return findAllIds
            .SelectMany(a => a.Value)
            .Where(a => !a.Equals(main))
            .Distinct()
            .ToDictionary(a => a.Type, a => a.Id);
    }
}