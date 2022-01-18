#if UNITY_ANDROID
using FPCSharpUnity.unity.Android.Bindings.android.content;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.app {

  [JavaBinding("android.app.Application")]
  public class Application : Context {
    public Application(AndroidJavaObject java) : base(java) {}
  }
}
#endif