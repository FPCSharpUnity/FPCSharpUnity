﻿#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.Extensions {
  public static class BuildTargetExts {
    public static RuntimePlatform toRuntimePlatform(this BuildTarget t) {
      #pragma warning disable SwitchEnumAnalyzer
      switch (t) {
        case BuildTarget.StandaloneWindows:
        case BuildTarget.StandaloneWindows64:
          return RuntimePlatform.WindowsPlayer;
        case BuildTarget.StandaloneLinux64:
#if !UNITY_2019_2_OR_NEWER
        case BuildTarget.StandaloneLinuxUniversal:
        case BuildTarget.StandaloneLinux:
#endif
          return RuntimePlatform.LinuxPlayer;
        case BuildTarget.iOS: return RuntimePlatform.IPhonePlayer;
        case BuildTarget.Android: return RuntimePlatform.Android;
        case BuildTarget.WebGL: return RuntimePlatform.WebGLPlayer;
#if !UNITY_2018_3_OR_NEWER
        case BuildTarget.PSP2: return RuntimePlatform.PSP2;
#endif
        case BuildTarget.PS4: return RuntimePlatform.PS4;
        case BuildTarget.XboxOne: return RuntimePlatform.XboxOne;
        case BuildTarget.tvOS: return RuntimePlatform.tvOS;
#if !UNITY_5_3_OR_NEWER
        case BuildTarget.PSM: return RuntimePlatform.PSM;
#endif
#if !UNITY_5_4_OR_NEWER
        case BuildTarget.WP8Player: return RuntimePlatform.WP8Player;
        case BuildTarget.BlackBerry: return RuntimePlatform.BlackBerryPlayer;
        case BuildTarget.StandaloneGLESEmu:
        case BuildTarget.WebPlayer:
        case BuildTarget.WebPlayerStreamed:
#endif
#if UNITY_5_5_OR_NEWER
#if !UNITY_2018_1_OR_NEWER
        case BuildTarget.N3DS:
#endif
#else
        case BuildTarget.PS3: return RuntimePlatform.PS3;
        case BuildTarget.XBOX360: return RuntimePlatform.XBOX360;
        case BuildTarget.Nintendo3DS:
#endif
#if UNITY_2017_3_OR_NEWER
        case BuildTarget.StandaloneOSX: return RuntimePlatform.OSXPlayer;
#else
        case BuildTarget.SamsungTV: return RuntimePlatform.SamsungTVPlayer;
        case BuildTarget.Tizen: return RuntimePlatform.TizenPlayer;
        case BuildTarget.StandaloneOSXUniversal: return RuntimePlatform.OSXPlayer;
        case BuildTarget.StandaloneOSXIntel:
        case BuildTarget.StandaloneOSXIntel64:
          return RuntimePlatform.OSXPlayer;
#endif
#if !UNITY_2018_1_OR_NEWER
        case BuildTarget.WiiU: return RuntimePlatform.WiiU;
#endif
        case BuildTarget.WSAPlayer:
          throw new ArgumentOutOfRangeException(
            nameof(t), t, $"Can't convert to {nameof(RuntimePlatform)}"
          );

        default:
          throw new ArgumentOutOfRangeException(
            nameof(t), t, $"Are you using obsolete {nameof(BuildTarget)}?"
          );
      }
      #pragma warning restore SwitchEnumAnalyzer
    }

    public static BuildTargetGroup toGroup(this BuildTarget t) {
      #pragma warning disable SwitchEnumAnalyzer
      switch (t) {
        case BuildTarget.StandaloneWindows: return BuildTargetGroup.Standalone;
        case BuildTarget.iOS: return BuildTargetGroup.iOS;
        case BuildTarget.Android: return BuildTargetGroup.Android;
#if !UNITY_2019_2_OR_NEWER
        case BuildTarget.StandaloneLinux: return BuildTargetGroup.Standalone;
        case BuildTarget.StandaloneLinuxUniversal: return BuildTargetGroup.Standalone;
#endif
        case BuildTarget.StandaloneWindows64: return BuildTargetGroup.Standalone;
        case BuildTarget.WebGL: return BuildTargetGroup.WebGL;
        case BuildTarget.WSAPlayer: return BuildTargetGroup.WSA;
        case BuildTarget.StandaloneLinux64: return BuildTargetGroup.Standalone;
#if !UNITY_2018_3_OR_NEWER
        case BuildTarget.PSP2: return BuildTargetGroup.PSP2;
#endif
        case BuildTarget.PS4: return BuildTargetGroup.PS4;
        case BuildTarget.XboxOne: return BuildTargetGroup.XboxOne;
        case BuildTarget.tvOS: return BuildTargetGroup.tvOS;
#if !UNITY_5_3_OR_NEWER
        case BuildTarget.PSM: return BuildTargetGroup.PSM;
#endif
#if !UNITY_5_4_OR_NEWER
        case BuildTarget.StandaloneGLESEmu: return BuildTargetGroup.Standalone;
        case BuildTarget.WebPlayer: return BuildTargetGroup.WebPlayer;
        case BuildTarget.WebPlayerStreamed: return BuildTargetGroup.WebPlayer;
        case BuildTarget.WP8Player: return BuildTargetGroup.WP8;
        case BuildTarget.BlackBerry: return BuildTargetGroup.BlackBerry;
#endif
#if UNITY_5_5_OR_NEWER
#if !UNITY_2018_1_OR_NEWER
        case BuildTarget.N3DS: return BuildTargetGroup.N3DS;
#endif
#else
        case BuildTarget.PS3: return BuildTargetGroup.PS3;
        case BuildTarget.XBOX360: return BuildTargetGroup.XBOX360;
        case BuildTarget.Nintendo3DS: return BuildTargetGroup.Nintendo3DS;
#endif
#if UNITY_2017_3_OR_NEWER
        case BuildTarget.StandaloneOSX: return BuildTargetGroup.Standalone;
#else
        case BuildTarget.StandaloneOSXUniversal: return BuildTargetGroup.Standalone;
        case BuildTarget.SamsungTV: return BuildTargetGroup.SamsungTV;
        case BuildTarget.Tizen: return BuildTargetGroup.Tizen;
        case BuildTarget.StandaloneOSXIntel64: return BuildTargetGroup.Standalone;
        case BuildTarget.StandaloneOSXIntel: return BuildTargetGroup.Standalone;
#endif
#if !UNITY_2018_1_OR_NEWER
        case BuildTarget.WiiU: return BuildTargetGroup.WiiU;
#endif
        default:
          throw new ArgumentOutOfRangeException(
            nameof(t), t, $"Are you using obsolete {nameof(BuildTarget)}?"
          );
      }
      #pragma warning restore SwitchEnumAnalyzer
    }
  }
}
#endif