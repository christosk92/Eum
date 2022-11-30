using System.Diagnostics.CodeAnalysis;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.Helpers;

namespace Eum.Connections.Spotify.Events
{
    public class NewPlaybackIdEvent : IGenericEvent
    {
        private readonly string sessionId;
        private readonly string playbackId;
        private readonly ITimeProvider _timeProvider;

        public NewPlaybackIdEvent([NotNull] string sessionId, [NotNull] string playbackId, ITimeProvider timeProvider)
        {
            this.sessionId = sessionId;
            this.playbackId = playbackId;
            _timeProvider = timeProvider;
        }

        public EventBuilder Build()
        {
            var @event = new EventBuilder(EventType.NEW_PLAYBACK_ID);
            @event.Append(playbackId).Append(sessionId)
                .Append(_timeProvider.CurrentTimeMillis().ToString());
            return @event;
        }
    }
}