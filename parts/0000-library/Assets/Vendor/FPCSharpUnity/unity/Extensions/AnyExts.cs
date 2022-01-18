using System;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static class AnyExts {
    public static A orElseIfNull<A>(this A a, Func<A> ifNull) where A : class =>
      F.isNull(a) ? ifNull() : a;

    public static A orElseIfNull<A>(this A a, A ifNull) where A : class =>
      F.isNull(a) ? ifNull : a;
  }
}