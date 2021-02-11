using System.Collections.Generic;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace VideoEffects.Helpers
{
    public static class Constants
    {
        public static MediaMemoryTypes GlobalSupportedMemoryTypes => MediaMemoryTypes.Gpu;

        public static IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => new List<VideoEncodingProperties>()
        {
            VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, 0, 0)
        };
    }
}
