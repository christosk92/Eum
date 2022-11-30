using System.Diagnostics.CodeAnalysis;
using Eum.Connections.Spotify.Helpers;
using Eum.Spotify.connectstate;
using Eum.Spotify.metadata;
using Google.Protobuf;

namespace Eum.Connections.Spotify.Playback.States
{
    public class ImageId 
    {
        public ImageId(string uri)
        {
            Uri = uri;
        }
        public string Uri { get; }
        public static void PutAsMetadata([NotNull] ProvidedTrack builder,
            [NotNull] ImageGroup group)
        {
            foreach (var image in group.Image)
            {
                String key;
                switch (image.Size)
                {
                    case Image.Types.Size.Default:
                        key = "image_url";
                        break;
                    case Image.Types.Size.Small:
                        key = "image_small_url";
                        break;
                    case Image.Types.Size.Large:
                        key = "image_large_url";
                        break;
                    case Image.Types.Size.Xlarge:
                        key = "image_xlarge_url";
                        break;
                    default:
                        continue;
                }

                builder.Metadata[key] = new ImageId(image.FileId).Uri;
            }
        }
        public ImageId(ByteString hexByteString) : this($"spotify:image:{hexByteString.ToByteArray().BytesToHex()}")
        { }
        
    }
}
