﻿using System.Collections.Immutable;
using FPCSharpUnity.unity.Data;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Platform {
  public interface IPlatformPackageManager {
    [PublicAPI] ImmutableSortedSet<string> packageNames { get; }
    [PublicAPI] bool hasAppInstalled(string bundleIdentifier);
    [PublicAPI] Option<ErrorMsg> openApp(string bundleIdentifier);
  }

  public static class PlatformPackageManager {
    public static readonly IPlatformPackageManager packageManager =
#if UNITY_ANDROID
      UnityEngine.Application.isEditor
        ? (IPlatformPackageManager) new NoOpPlatformPackageManager()
        : new AndroidPlatformPackageManager();
#else
      new NoOpPlatformPackageManager();
#endif
  }

  class NoOpPlatformPackageManager : IPlatformPackageManager {
    public ImmutableSortedSet<string> packageNames => ImmutableSortedSet<string>.Empty;
    public bool hasAppInstalled(string bundleIdentifier) => false;
    public Option<ErrorMsg> openApp(string bundleIdentifier) => None._;
  }
}