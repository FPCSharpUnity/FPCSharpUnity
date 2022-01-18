#if UNITY_ANDROID
using FPCSharpUnity.unity.Android.Bindings.android.content;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.support.v4.content {
  public class ContextCompat {
    static readonly AndroidJavaClass klass =
      new AndroidJavaClass("android.support.v4.content.ContextCompat");

    public static bool checkSelfPermission(
      string permission, Context context = null
    ) => klass.CallStatic<int>(
      "checkSelfPermission", (context ?? AndroidActivity.activity).java, permission
    ) == 0;
  }
}
#endif