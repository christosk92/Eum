using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eum.Connections.Spotify.Playback.Crossfade;

public class FadeInterval
{
    public FadeInterval(int start, int duration, IGainInterpolator interpolator)
    {
        Start = start;
        Duration = duration;
        Interpolator = interpolator;
    }

    public virtual int Start { get; }
    public virtual int End => Start + Duration;
    public int Duration { get; }
    
    [JsonConverter(typeof(IGainInterpolatorToStringConverter))]
    public IGainInterpolator Interpolator { get; }
    
    public virtual float Interpolate(int trackPos) {
        float pos = ((float) trackPos - Start) / Duration;
        pos = Math.Min(pos, 1);
        pos = Math.Max(pos, 0);
        return Interpolator.Interpolate(pos);
    }
    
    public override string ToString() {
        return "FadeInterval{start=" + Start + ", duration=" + Duration + ", interpolator=" + Interpolator + '}';
    }
}

public class IGainInterpolatorToStringConverter : JsonConverter<IGainInterpolator>
{
    public override IGainInterpolator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
            throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IGainInterpolator value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.GetType().Name);
    }
}

public class PartialFadeInterval : FadeInterval
{
    private int partialStart = -1;

    public PartialFadeInterval(int duration, IGainInterpolator interpolator) : base(-1, duration, interpolator)
    {
    }

    public override int Start => partialStart;

    public override int End => partialStart + Duration;

    public int EndAt(int now)
    {
        partialStart = now;
        return End;
    }

    public override float Interpolate(int trackPos)
    {
        return base.Interpolate(trackPos -1 - partialStart);
    }

    public override string ToString()
    {
        return "PartialFadeInterval{duration=" + Duration + ", interpolator=" + Interpolator + "}";
    }
}

public interface IGainInterpolator
{
    float Interpolate(float pos);
    
    float Last { get; }
}