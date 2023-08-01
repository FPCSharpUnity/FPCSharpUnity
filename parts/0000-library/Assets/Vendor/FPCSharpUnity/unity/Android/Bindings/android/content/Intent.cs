#if UNITY_ANDROID
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Android.Bindings.android.net;
using FPCSharpUnity.unity.Android.Bindings.android.os;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.content {
  // https://developer.android.com/reference/android/content/Intent.html
  [JavaBinding("android.content.Intent"), PublicAPI]
  public class Intent : Binding {
    public Intent(AndroidJavaObject java) : base(java) { }

    /// https://developer.android.com/reference/android/content/Intent#getAction()
    public Option<string> getAction() => java.Call<string>("getAction").opt();
    
    /// https://developer.android.com/reference/android/content/Intent#getData()
    public Option<Uri> getData() {
      var jUri = java.cjo("getData");
      return jUri == null ? None._ : Some.a(new Uri(jUri));
    }

    /// https://developer.android.com/reference/android/content/Intent#getDataString()
    public Option<string> getDataString() => java.Call<string>("getDataString").opt();

    /// https://developer.android.com/reference/android/content/Intent#getExtras()
    public Option<Bundle> getExtras() => java.cjo("getExtras").opt().mapM(jo => new Bundle(jo));
  }
}
#endif