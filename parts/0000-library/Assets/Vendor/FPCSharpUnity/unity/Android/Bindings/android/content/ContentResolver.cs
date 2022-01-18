#if UNITY_ANDROID
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.content {
  public class ContentResolver : Binding {
    public ContentResolver(AndroidJavaObject java) : base(java) {}
  }
}
#endif