#if UNITY_ANDROID
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.util {
  public class Iterator : Binding {
    public Iterator(AndroidJavaObject java) : base(java) {}

    public bool hasNext => java.Call<bool>("hasNext");
    public AndroidJavaObject next() => java.cjo("next");
  }
}
#endif