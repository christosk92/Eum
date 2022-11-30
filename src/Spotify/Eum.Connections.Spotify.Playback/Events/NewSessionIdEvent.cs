using System.Diagnostics.CodeAnalysis;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Playback.States;

namespace Eum.Connections.Spotify.Events
{
    internal sealed class NewSessionIdEvent : IGenericEvent
    {
        private readonly string sessionId;
        private readonly StateWrapper _state;
        private readonly ITimeProvider _timeProvider;
        public NewSessionIdEvent(
            string sessionId,
            StateWrapper state,
            ITimeProvider timeProvider)
        {
            this.sessionId = sessionId;
            _state = state;
            _timeProvider = timeProvider;
        }


        public EventBuilder Build()
        {
            var contextUri = _state.ContextUri;

            var @event = new EventBuilder(EventType.NEW_SESSION_ID);
            @event.Append(sessionId);
            @event.Append(contextUri);
            @event.Append(contextUri);
            @event.Append(_timeProvider.CurrentTimeMillis().ToString());
            @event.Append("").Append(_state.ContextSize.ToString());
            @event.Append(_state.State.ContextUrl);
            return @event;
        }
    }
}