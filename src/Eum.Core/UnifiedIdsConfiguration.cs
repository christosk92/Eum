// using Eum.Core.Contracts;
//
// namespace Eum.Core;
//
// internal sealed class UnifiedIdsConfiguration : IUnifiedIdsConfiguration
// {
//     private IList<HashSet<CoreId>> _ids = new List<HashSet<CoreId>>();
//     public void LoadData(List<HashSet<CoreId>> ids)
//     {
//         _ids = ids;
//     }
//
//     public HashSet<CoreId> MergeId(CoreId externalId, CoreId toId)
//     {
//         var hashset =
//             _ids.FirstOrDefault(a =>
//             {
//                 return a.Any(k => k.Equals(externalId))
//                        || a.Any(k => k.Equals(toId));
//             });
//        if (hashset == null)
//        {
//            hashset = new HashSet<CoreId>(new[]
//            {
//                externalId,
//                toId
//            });
//            _ids.Add(hashset);
//        }
//        else
//        {
//            hashset.Add(externalId);
//            hashset.Add(toId);
//        }
//
//        return hashset;
//     }
//
//     public CoreId? GetIdForOtherId(CoreId id, CoreType forType)
//     {
//         var hashset =
//             _ids.FirstOrDefault(a =>
//             {
//                 return a.Any(k => k.Equals(id));
//             });
//
//         var idToFind = hashset?.FirstOrDefault(a => a.Type == forType);
//         return idToFind?.Id != null ? idToFind : null;
//     }
// }