#if UNITY_ANDROID
using System;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Android.Bindings.fp_csharp_unity.unity.fns {
  public class Fn1<A> : JavaProxy {
    readonly Func<A> f;

    public Fn1(Func<A> f) : base("fp_csharp_unity.unity.fns.Fn1") { this.f = f; }

    [UsedImplicitly]
    A run() => f();
  }
}
#endif