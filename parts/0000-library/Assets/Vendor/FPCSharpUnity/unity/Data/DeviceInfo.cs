#if UNITY_ANDROID && !UNITY_EDITOR
using FPCSharpUnity.unity.Android.Bindings.android.os;
#endif
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Data {
  [Record]
  public partial struct DeviceInfo {
    public readonly string manufacturer, modelCode;

    public static Option<DeviceInfo> create() {
#if UNITY_ANDROID && !UNITY_EDITOR
      return Some.a(new DeviceInfo(manufacturer: Build.MANUFACTURER, modelCode: Build.DEVICE));
#endif
      return F.none<DeviceInfo>();
    }
  }
}