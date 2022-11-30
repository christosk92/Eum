using System;
using System.Threading;
using System.Threading.Tasks;
using Eum.Logging;

namespace Eum.Connections.Spotify.Playback.Playback;

public class PlayerQueue : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
    public PlayerQueueEntry? Head { get; private set; }

    public void Add(PlayerQueueEntry entry)
    {
        if (Head == null) Head = entry;
        else Head.SetNext(entry);

        Task.Run(async () => await entry.Do(_cancellation.Token), _cancellation.Token);
        S_Log.Instance.LogInfo("Added to queue: " + entry);
    }

    public void Swap(PlayerQueueEntry oldEntry, PlayerQueueEntry newEntry)
    {
        if (Head == null) return;

        bool swapped = false;
        if (Head == oldEntry)
        {
            Head = newEntry;
            Head.Next = oldEntry.Next;
            Head.Prev = oldEntry.Prev;
            swapped = true;
        }
        else
        {
            swapped = Head.Swap(oldEntry, newEntry);
        }

        oldEntry.Dispose();
        if (swapped)
        {
            Task.Run(async () => await newEntry.Do(_cancellation.Token), _cancellation.Token);
            S_Log.Instance.LogInfo($"Swapped {oldEntry} with {newEntry}. New head is {Head}");
        }
    }

    public void Remove(PlayerQueueEntry entry)
    {
        if (Head == null) return;

        var removed = false;
        if (Head == entry)
        {
            var tmp = Head;
            Head = tmp.Next;
            tmp.Dispose();
            removed = true;
        }
        else
        {
            removed = Head.Remove(entry);
        }

        if (removed)
        {
            S_Log.Instance.LogInfo("Removed from queue: " + entry);
        }
    }

    public bool Advance()
    {
        if (Head == null || Head.Next == null)
            return false;

        var tmp = Head.Next;
        Head.Next = null;
        Head.Prev = null;
        tmp.Prev = Head;
        Head = tmp;
        return true;
    }

    public void Dispose()
    {
        if (Head != null)
        {
            Head.Clear();

            _cancellation.Cancel();
            S_Log.Instance.LogInfo("Queue has been cleared.");
        }
    }
}