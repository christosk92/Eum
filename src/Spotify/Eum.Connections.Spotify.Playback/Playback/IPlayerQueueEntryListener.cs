using System;
using System.Collections.Generic;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback.Audio;

namespace Eum.Connections.Spotify.Playback.Playback;

public interface IPlayerQueueEntryListener
{
    /// <summary>
    /// An error occurred while playing the track
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    /// <param name="ex">The exception thrown.</param>
    void PlaybackError(PlayerQueueEntry entry, Exception ex);

    /// <summary>
    /// The playback of the current entry ended gracefully.
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    void PlaybackEnded(PlayerQueueEntry entry);
    /// <summary>
    /// The playback halted while trying to receive a chunk.
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="chunk"></param>
    void PlaybackHalted(PlayerQueueEntry entry, int chunk);

    /// <summary>
    /// The playback resumed from halt.
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    /// <param name="chunk">The chunk that was being retrieved.</param>
    /// <param name="diff">The time taken to retrieve the chunk.</param>
    void PlaybackResumed(PlayerQueueEntry entry, int chunk, int diff);

    /// <summary>
    /// Notify that a previously request instant has been reached. This is called from the TaskScheduler. Be careful.
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    /// <param name="callbackId">The callback ID for the instant.</param>
    /// <param name="exactTime">The exact time the instant was reached.</param>
    void InstantReached(PlayerQueueEntry entry, int callbackId, int exactTime);

    /// <summary>
    /// The track started loading.
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    void StartedLoading(PlayerQueueEntry entry);

    /// <summary>
    /// The track failed to load.
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    /// <param name="exception">The exception thrown.</param>
    /// <param name="retried">Whether this is the second time an error occurs.</param>
    void LoadingError(PlayerQueueEntry entry, Exception exception, bool retried);
    
    /// <summary>
    /// The track finished loading.
    /// </summary>
    /// <param name="entry">The <see cref="PlayerQueueEntry"/> that called this.</param>
    /// <param name="metadata">The <see cref="MetadataWrapper"/> metadata of the object.</param>
    void FinishedLoading(PlayerQueueEntry entry, MetadataWrapper metadata);


    /// <summary>
    /// Get the metadata for this content.
    /// </summary>
    /// <param name="id">The content.</param>
    /// <returns>A dictionary containing all the metadata related.</returns>
    IReadOnlyDictionary<string, string>? MetadataFor(SpotifyId id);
}