#if UNITY_ANDROID
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.io {
  public class Writer : Binding {
    public Writer(AndroidJavaObject java) : base(java) {}
  }
}
#endif