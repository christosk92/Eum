using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.Events;
using Eum.Connections.Spotify.Helpers;
using Eum.Logging;
using SpotifyTcp.Models;

namespace Eum.Connections.Spotify.Clients
{
    public class EventBuilder
    {
        private readonly MemoryStream body = new MemoryStream(256);

        public EventBuilder([NotNull] EventType type)
        {
            AppendNoDelimiter(type.Id.ToString());
            Append(type.Unknown.ToString());
        }

        public EventBuilder Append(string str)
        {
            body.WriteByte(0x09);
            AppendNoDelimiter(str);
            return this;
        }

        private void AppendNoDelimiter( String str)
        {
            if (str == null) str = "";
            var bytesToWrite = Encoding.UTF8.GetBytes(str);
            body.Write(bytesToWrite, 0, bytesToWrite.Length);
        }

        internal static string ToString([NotNull] byte[] body)
        {
            var result = new StringBuilder();
            foreach (var b in body)
            {
                if (b == 0x09) result.Append('|');
                else result.Append((char) b);
            }

            return result.ToString();
        }
        public byte[] ToArray()
        {
            return body.ToArray();
        }
    }

    public class EventService : IEventService, IDisposable
    {
        private readonly IMercuryClient _session;
        private readonly ITimeProvider _timeProvider;
        public EventService(IMercuryClient session, ITimeProvider timeProvider)
        {
            _session = session;
            _timeProvider = timeProvider;
        }

        public async Task Language([NotNull] string lang)
        {
            EventBuilder @event = new EventBuilder(EventType.LANGUAGE);
            @event.Append(lang);
            await SendEvent(@event);
        }

        public async Task SendEvent([NotNull] EventBuilder builder)
        {
            try
            {
                var body = builder.ToArray();
                var req = new RawMercuryRequest("hm://event-service/v1/events", "POST");
                req.Payload.Add(body);
                req.AddUserField("Accept-Language", "en");
                req.AddUserField("X-ClientTimeStamp", _timeProvider.CurrentTimeMillis().ToString());

                var resp = await _session.SendAndReceiveResponseAsync(req);
                S_Log.Instance.LogInfo(
                    $"Event sent. body: {EventBuilder.ToString(body)}, result: {resp.StatusCode.ToString()}");
            }
            catch (IOException ex)
            {
                S_Log.Instance.LogError("Failed sending event: " + builder, ex);
            }
        }

        public virtual void Dispose(bool dispose)
        {

        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    public interface IEventService
    {
        Task SendEvent([NotNull] EventBuilder builder);
    }
}
