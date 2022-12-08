using System.Diagnostics.CodeAnalysis;
using Eum.Connections.Spotify.Playback.Metrics;
using Eum.Connections.Spotify.Playback.States;

namespace Eum.Connections.Spotify.Playback.Transitions
{
    internal class TransitionInfo

    {
        /// <summary>
        /// How the next track started
        /// </summary>
        internal readonly PlaybackMetricsReason StartedReason;

        /// <summary>
        /// How the previous track ended
        /// </summary>
        internal readonly PlaybackMetricsReason EndedReason;

        /// <summary>
        /// When the previous track ended
        /// </summary>
        internal int EndedWhen = -1;

        internal TransitionInfo(
             PlaybackMetricsReason endedReason,
             PlaybackMetricsReason startedReason)
        {
            StartedReason = startedReason;
            EndedReason = endedReason;
        }

        /// <summary>
        /// Context changed.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="withSkip"></param>
        /// <returns></returns>
        internal static TransitionInfo ContextChange(
             StateWrapper state, bool withSkip)
        {
            var trans = new TransitionInfo(PlaybackMetricsReason.END_PLAY,
                withSkip
                    ? PlaybackMetricsReason.CLICK_ROW
                    : PlaybackMetricsReason.PLAY_BTN);
            if (state.CurrentPlayable != null) trans.EndedWhen = (int)state.GetPosition();
            return trans;
        }

        /// <summary>
        /// Skipping to another track in the same context.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal static TransitionInfo SkipTo( StateWrapper state)
        {
            var trans = new TransitionInfo(
                PlaybackMetricsReason.END_PLAY,
                PlaybackMetricsReason.CLICK_ROW);
            if (state.CurrentPlayable != null) trans.EndedWhen = (int)state.GetPosition();
            return trans;
        }

        /// <summary>
        /// Skipping to previous track.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal static TransitionInfo SkippedPrev( StateWrapper state)
        {
            var trans = new TransitionInfo(PlaybackMetricsReason.BACK_BTN, PlaybackMetricsReason.BACK_BTN);
            if (state.CurrentPlayable != null) trans.EndedWhen = (int) state.GetPosition();
            return trans;
        }

        /// <summary>
        /// Skipping to next track.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal static TransitionInfo SkippedNext( StateWrapper state)
        {
            var trans = new TransitionInfo(PlaybackMetricsReason.FORWARD_BTN, PlaybackMetricsReason.FORWARD_BTN);
            if (state.CurrentPlayable != null) trans.EndedWhen = (int)state.GetPosition();
            return trans;
        }
    }
}
