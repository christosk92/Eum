using System.Collections.Generic;
using System.Linq;

namespace Eum.Connections.Spotify.Playback.Helpers;

public static class CollectionHelpers
{
    public static bool Swap<T>(this IEnumerable<T> objectArray, int x, int y)
    {

        // check for out of range
        var enumerable = objectArray as T[] ?? objectArray.ToArray();
        if (enumerable.Length <= y || enumerable.Length <= x) return false;


        // swap index x and y
        (enumerable[x], enumerable[y]) = (enumerable[y], enumerable[x]);
        
        return true;
    }
}