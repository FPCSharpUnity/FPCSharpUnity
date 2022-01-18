#if UNITY_ANDROID
using FPCSharpUnity.core.functional;
using System;
using FPCSharpUnity.unity.Functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.java.lang {
  public class System : IDisposable {
    public static A with<A>(Func<System, A> f) {
      using (var sys = new System(new AndroidJavaClass("java.lang.System"))) {
        return f(sys);
      }
    }

    readonly AndroidJavaClass klass;

    System(AndroidJavaClass klass) { this.klass = klass; }

    public void Dispose() => klass.Dispose();

    public Option<string> getProperty(string key) =>
      F.opt(klass.CallStatic<string>("getProperty", key));
  }
}
#endif