using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Enums;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.metadata;
using Eum.Spotify.transfer;
using Google.Protobuf;
using Google.Protobuf.Collections;
using ContextPlayerOptions = Eum.Spotify.connectstate.ContextPlayerOptions;
using PlayOrigin = Eum.Spotify.connectstate.PlayOrigin;

namespace Eum.Connections.Spotify.Playback.States;

internal static class ProtoUtils
{
    public static ProvidedTrack ToProvidedTrack(ContextTrack? track, string contextUri)
    {
        if (track == null) return null;

        var builder = new ProvidedTrack
        {
            Provider = "context"
        };
        if (track.HasUid) builder.Uid = track.Uid;
        if (track.HasUri && !string.IsNullOrEmpty(track.Uri))
        {
            builder.Uri = track.Uri;
        }
        else if (track.HasGid)
        {
            var id = new SpotifyId(track.Gid, SpotifyId.InferUriType(contextUri));
            builder.Uri = id.Uri;
        }


        if (track.Metadata.ContainsKey("album_uri"))
            builder.AlbumUri = track.Metadata["album_uri"];

  

        if (track.Metadata.ContainsKey("artist_uri"))
            builder.ArtistUri = track.Metadata["artist_uri"];

        foreach (var metadata in track.Metadata)
        {
            builder.Metadata[metadata.Key] = metadata.Value;
        }

        return builder;
    }

    public static ContextPage JsonToContextPage(JsonElement page)
    {
        var contextPage = new ContextPage();
        if (page.TryGetProperty("next_page_url", out var pageType))
        {
            contextPage.NextPageUrl = pageType.GetString();
        }

        if (page.TryGetProperty("page_url", out var pageUrl))
        {
            contextPage.PageUrl = pageUrl.GetString();
        }

        if (page.TryGetProperty("tracks", out var tracksData))
        {
            using var enumerator = tracksData.EnumerateArray();
            foreach (var track in enumerator)
            {
                contextPage.Tracks.Add(JsonToContextTrack(track));
            }
        }

        return contextPage;
    }

    public static ContextTrack JsonToContextTrack(JsonElement obj)
    {
        var contextTrack = new ContextTrack();
        if (obj.TryGetProperty("uid", out var uid))
        {
            contextTrack.Uid = uid.GetString();
        }

        if (obj.TryGetProperty("uri", out var uri))
        {
            contextTrack.Uri = uri.GetString();

            var spotifyId = new SpotifyId(contextTrack.Uri);
            var gid = ByteString.CopyFrom(Utils.HexToBytes(spotifyId.HexId()));
            contextTrack.Gid = gid;
        }

        if (obj.TryGetProperty("metadata", out var metadata))
        {
            using var enumerator = metadata.EnumerateObject();
            foreach (var property in enumerator)
            {
                contextTrack.Metadata[property.Name] = property.Value.GetString();
            }
        }

        return contextTrack;
    }

    public static void PutFilesAsMetadata(ProvidedTrack builder, RepeatedField<AudioFile> trackFile)
    {
        var formats = new List<AudioFile.Types.Format>();
        foreach (var file in trackFile)
        {
            if (file.HasFormat) formats.Add(file.Format);
        }

        if (formats.Count > 0) builder.Metadata["available_file_formats"] = JsonSerializer.Serialize(formats);
    }

    public static bool IsTrack(ProvidedTrack stateTrack, Track track)
    {
        return stateTrack.Uri == (new SpotifyId(track.Gid, EntityType.Track)).Uri;
    }

    public static PlayOrigin ConvertPlayOrigin(global::Eum.Spotify.context.PlayOrigin? po)
    {
        if (po == null) return null;

        var builder = new PlayOrigin();

        if (po.HasFeatureIdentifier) builder.FeatureIdentifier = po.FeatureIdentifier;
        if (po.HasFeatureVersion) builder.FeatureVersion = po.FeatureVersion;
        if (po.HasViewUri) builder.ViewUri = po.ViewUri;
        if (po.HasExternalReferrer) builder.ExternalReferrer= po.ExternalReferrer;
        if (po.HasReferrerIdentifier) builder.ReferrerIdentifier = po.ReferrerIdentifier;
        if (po.HasDeviceIdentifier) builder.DeviceIdentifier = po.DeviceIdentifier;

        builder.FeatureClasses.AddRange(po.FeatureClasses);

        return builder;
    }

    public static ContextPlayerOptions ConverterPlayerOptions(global::Eum.Spotify.context.ContextPlayerOptions? options)
    {
        if (options == null) return null;

        var builder = new ContextPlayerOptions();
        if (options.HasRepeatingContext) builder.RepeatingContext = options.RepeatingContext;
        if (options.HasRepeatingTrack) builder.RepeatingTrack = options.RepeatingTrack;
        if (options.HasShufflingContext) builder.ShufflingContext = options.ShufflingContext;

        return builder;
    }
    
    public static bool trackEquals(ContextTrack first, ContextTrack second) {
        if (first == null || second == null) return false;
        if (first == second) return true;

        if (first.HasUri && !string.IsNullOrEmpty(first.Uri) && second.HasUri && !string.IsNullOrEmpty(second.Uri))
            return first.Uri.Equals(second.Uri);

        if (first.HasGid && second.HasGid)
            return first.Gid.Equals(second.Gid);

        if (first.HasUid && !string.IsNullOrEmpty(first.Uid) && second.HasUid && !string.IsNullOrEmpty(second.Uid))
            return first.Uid.Equals(second.Uid);

        return false;
    }

    public static void EnrichTrack(
         ContextTrack subject, 
         ContextTrack track)
    {
        if (subject.HasUri && track.HasUri && !string.IsNullOrEmpty(subject.Uri) && !string.IsNullOrEmpty(track.Uri) 
            && !Equals(subject.Uri, track.Uri))
            throw new Exception("Illegal Argument");

        if (subject.HasGid && track.HasGid && !Equals(subject.Gid, track.Gid))
            throw new Exception("Illegal Argument");

        foreach (var kvp in track.Metadata)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            subject.Metadata[key] = value;
        }
    }
    public static void EnrichTrack(
         ProvidedTrack subject,
         ContextTrack track)
    {
        if (track.HasUri && !object.Equals(subject.Uri, track.Uri))
            throw new Exception("Illegal Argument");

        foreach (var kvp in track.Metadata)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            subject.Metadata[key] = value;
        }
    }

    public static PlayOrigin JsonToPlayOrigin(JsonElement getPlayOrigin)
    {
        var builder = new PlayOrigin();
        
        if (getPlayOrigin.TryGetProperty("feature_identifier", out var featureIdentifier))
        {
            builder.FeatureIdentifier = featureIdentifier.GetString();
        }
        if(getPlayOrigin.TryGetProperty("feature_version", out var featureVersion))
        {
            builder.FeatureVersion = featureVersion.GetString();
        }
        if(getPlayOrigin.TryGetProperty("view_uri", out var viewUri))
        {
            builder.ViewUri = viewUri.GetString();
        }
        if(getPlayOrigin.TryGetProperty("external_referrer", out var externalReferrer))
        {
            builder.ExternalReferrer = externalReferrer.GetString();
        }
        if(getPlayOrigin.TryGetProperty("referrer_identifier", out var referrerIdentifier))
        {
            builder.ReferrerIdentifier = referrerIdentifier.GetString();
        }
        if(getPlayOrigin.TryGetProperty("device_identifier", out var deviceIdentifier))
        {
            builder.DeviceIdentifier = deviceIdentifier.GetString();
        }

        return builder;
    }

    public static ContextPlayerOptions JsonToPlayerOptions(ref JsonElement getPlayerOptions, ContextPlayerOptions? old)
    {
        var builder = old ?? new ContextPlayerOptions();
        
        if (getPlayerOptions.TryGetProperty("repeating_context", out var repeatingContext))
        {
            builder.RepeatingContext = repeatingContext.GetBoolean();
        }
        if (getPlayerOptions.TryGetProperty("repeating_track", out var repeatingTrack))
        {
            builder.RepeatingTrack = repeatingTrack.GetBoolean();
        }
        if (getPlayerOptions.TryGetProperty("shuffling_context", out var shufflingContext))
        {
            builder.ShufflingContext = shufflingContext.GetBoolean();
        }

        return builder;
    }

    public static Context JsonToContext(ref JsonElement contextJson)
    {
        var builder = new Context();
        
        if (contextJson.TryGetProperty("uri", out var uri))
        {
            builder.Uri = uri.GetString();
        }

        if (contextJson.TryGetProperty("url", out var url))
        {
            builder.Url = url.GetString();
        }
        if (contextJson.TryGetProperty("metadata", out var metadata))
        {
            using var enumerator = metadata.EnumerateObject();
            foreach (var kvp in enumerator)
            {
                var key = kvp.Name;
                var value = kvp.Value;
                builder.Metadata[key] = value.ToString();
            }
        }

        if (contextJson.TryGetProperty("pages", out var pages))
        {
            using var enumerator = pages.EnumerateArray();
            foreach (var element in enumerator)
            {
                var page = JsonToContextPage(element);
                builder.Pages.Add(page);
            }
        }

        return builder;
    }

    public static JsonElement CraftContextStateCombo(PlayerState ps, List<ContextTrack> tracks)
    {
        var page = new
        {
            page_url = string.Empty,
            next_page_url = string.Empty,
            tracks = tracks.Select(s => new
            {
                uri = s.Uri,
                uid = s.Uid,
                metadata = s.Metadata
            }).ToList(),
            metadata = ps.PageMetadata
        };
        var context = new
        {
            uri = ps.ContextUri,
            url = ps.ContextUrl,
            metadata = ps.ContextMetadata,
            pages = new []
            {
                page
            },
        };

        var options = new
        {
            shuffling_context = ps.Options.ShufflingContext,
            repeating_context = ps.Options.RepeatingContext,
            repeating_track = ps.Options.RepeatingTrack,
        };

        var state = new
        {
            options = options,
            skip_to = new object(),
            track = ps.Track
        };

        var result = new
        {
            context = context,
            state = state
        };
        var serialized =
            JsonSerializer.SerializeToElement(result);

        return serialized;
    }
}