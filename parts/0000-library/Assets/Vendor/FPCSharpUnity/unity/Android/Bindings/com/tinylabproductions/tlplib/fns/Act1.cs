#if UNITY_ANDROID
using System;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Android.Bindings.fp_csharp_unity.unity.fns {
  public class Act1<A> : JavaProxy {
    readonly Action<A> act;

    public Act1(Action<A> act) : base("fp_csharp_unity.unity.fns.Act1") { this.act = act; }

    [UsedImplicitly]
    void run(A a) => act(a);
  }
}
#endif