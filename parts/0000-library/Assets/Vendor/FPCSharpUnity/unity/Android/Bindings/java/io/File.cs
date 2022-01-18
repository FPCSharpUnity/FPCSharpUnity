#if UNITY_ANDROID
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.io {
  public class File : Binding {
    public File(AndroidJavaObject java) : base(java) {}

    public string getPath() => java.Call<string>("getPath");
  }
}
#endif