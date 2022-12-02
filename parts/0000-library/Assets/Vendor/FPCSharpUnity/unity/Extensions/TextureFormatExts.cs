using ExhaustiveMatching;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class TextureFormatExts {
    public static bool isCompressed(this TextureFormat format) {
      switch (format) {
        case TextureFormat.Alpha8:
        case TextureFormat.RGB24:
        case TextureFormat.RGBA32:
        case TextureFormat.ARGB32:
        case TextureFormat.R16:
        case TextureFormat.BGRA32:
        case TextureFormat.RG32:
        case TextureFormat.RGB48:
        case TextureFormat.RGBA64:
        case TextureFormat.RG16:
        case TextureFormat.R8:
          return false;
        case TextureFormat.RHalf:
        case TextureFormat.RGHalf:
        case TextureFormat.RGBAHalf:
        case TextureFormat.RGBAFloat:
        case TextureFormat.RFloat:
        case TextureFormat.RGFloat:
        case TextureFormat.RGB565:
        case TextureFormat.ARGB4444:
        case TextureFormat.RGBA4444:
        case TextureFormat.DXT1:
        case TextureFormat.DXT5:
        case TextureFormat.YUY2:
        case TextureFormat.RGB9e5Float:
        case TextureFormat.BC4:
        case TextureFormat.BC5:
        case TextureFormat.BC6H:
        case TextureFormat.BC7:
        case TextureFormat.DXT1Crunched:
        case TextureFormat.DXT5Crunched:
        case TextureFormat.PVRTC_RGB2:
        case TextureFormat.PVRTC_RGBA2:
        case TextureFormat.PVRTC_RGB4:
        case TextureFormat.PVRTC_RGBA4:
        case TextureFormat.ETC_RGB4:
        case TextureFormat.EAC_R:
        case TextureFormat.EAC_R_SIGNED:
        case TextureFormat.EAC_RG:
        case TextureFormat.EAC_RG_SIGNED:
        case TextureFormat.ETC2_RGB:
        case TextureFormat.ETC2_RGBA1:
        case TextureFormat.ETC2_RGBA8:
        case TextureFormat.ASTC_4x4:
        case TextureFormat.ASTC_5x5:
        case TextureFormat.ASTC_6x6:
        case TextureFormat.ASTC_8x8:
        case TextureFormat.ASTC_10x10:
        case TextureFormat.ASTC_12x12:
        case TextureFormat.ETC_RGB4Crunched:
        case TextureFormat.ETC2_RGBA8Crunched:
        case TextureFormat.ASTC_HDR_4x4:
        case TextureFormat.ASTC_HDR_5x5:
        case TextureFormat.ASTC_HDR_6x6:
        case TextureFormat.ASTC_HDR_8x8:
        case TextureFormat.ASTC_HDR_10x10:
        case TextureFormat.ASTC_HDR_12x12:
          return true;
        default:
          throw ExhaustiveMatch.Failed(format);
      }
    }
  }
}