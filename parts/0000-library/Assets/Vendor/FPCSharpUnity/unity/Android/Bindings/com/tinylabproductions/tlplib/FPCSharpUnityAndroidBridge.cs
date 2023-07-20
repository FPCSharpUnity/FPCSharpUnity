#if UNITY_ANDROID
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Android.Bindings.android.telephony;
using FPCSharpUnity.unity.Android.Bindings.java.lang;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Threads;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.fp_csharp_unity.unity {
  [PublicAPI] public static class FPCSharpUnityAndroidBridge {
    static readonly AndroidJavaClass klass = new AndroidJavaClass("fp_csharp_unity.unity.Bridge");

    static Option<bool> _isTablet = F.none<bool>();

    public static bool isTablet { get {
      if (_isTablet.isNone) {
        // cache result
        _isTablet = Some.a(klass.CallStatic<bool>("isTablet"));
      }
      return _isTablet.get;
    } }

    public static void sharePNG(string path, string title, string sharerText) {
      klass.CallStatic("sharePNG", path, title, sharerText);
    }
    
    public static Future<Either<string, Option<string>>> countryCodeFromLastKnownLocation { get {
      return Future.async<Either<string, Option<string>>>(p => new JThread(() => {
        Either<string, Option<string>> ret;
        try {
          ret = Either<string, Option<string>>.Right(
            TelephonyManager.jStringToCountryCode(
              klass.CallStatic<string>("countryCodeFromLastKnownLocation")
            )
          );
        }
        catch (AndroidJavaException e) {
          ret = Either<string, Option<string>>.Left(
            $"Error in {nameof(FPCSharpUnityAndroidBridge)}.{nameof(countryCodeFromLastKnownLocation)}: {e}"
          );
        }
        OnMainThread.run(() => p.complete(ret));
      }).start());
    } }
  }
}
#endif