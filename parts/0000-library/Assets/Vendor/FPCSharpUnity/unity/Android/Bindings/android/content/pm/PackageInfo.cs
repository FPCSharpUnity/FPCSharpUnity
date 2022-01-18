#if UNITY_ANDROID
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.content.pm {
  public class PackageInfo : Binding {
    public readonly string packageName;

    public PackageInfo(AndroidJavaObject java) : base(java) {
      // Cache the field.
      packageName = java.Get<string>("packageName");
    }

    // https://developer.android.com/reference/android/content/pm/PackageInfo.html#requestedPermissions
    public string[] requestedPermissions =>
      java.Get<string[]>("requestedPermissions").orElseIfNull(F.emptyArray<string>());
  }
}
#endif