#if UNITY_ANDROID
using FPCSharpUnity.core.functional;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Android;
using FPCSharpUnity.unity.Android.Bindings.android.content.pm;
using FPCSharpUnity.unity.Data;

namespace FPCSharpUnity.unity.Platform {
  class AndroidPlatformPackageManager : IPlatformPackageManager {
    public ImmutableSortedSet<string> packageNames { get; }

    public AndroidPlatformPackageManager() {
      packageNames =
        AndroidActivity.packageManager
          .getInstalledPackages(PackageManager.GetPackageInfoFlags.GET_ACTIVITIES)
          .Select(package => package.packageName)
          .ToImmutableSortedSet();
    }

    public bool hasAppInstalled(string bundleIdentifier) => packageNames.Contains(bundleIdentifier);
    public Option<ErrorMsg> openApp(string bundleIdentifier) =>
      AndroidActivity.packageManager.openApp(bundleIdentifier);
  }
}
#endif