#if UNITY_ANDROID
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.accounts {
  public class Account : Binding {
    public Account(AndroidJavaObject java) : base(java) {}

    public string name => java.Get<string>("name");

    public string type => java.Get<string>("type");
  }
}
#endif