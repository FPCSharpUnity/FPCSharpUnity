#if UNITY_ANDROID
using FPCSharpUnity.unity.Android.Bindings.android.content;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.fp_csharp_unity.unity.referrer {
  public class InstallReferrerReceiver {
    static readonly AndroidJavaClass klass;
    public static readonly string PREF_REFERRER;

    static InstallReferrerReceiver() {
      klass = new AndroidJavaClass(
        "fp_csharp_unity.unity.referrer.InstallReferrerReceiver"
      );
      PREF_REFERRER = klass.GetStatic<string>("PREF_REFERRER");
    }

    public static SharedPreferences preferences(Context ctx) =>
      new SharedPreferences(klass.csjo("getPrefs", ctx.java));
  }
}
#endif