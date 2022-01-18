using System;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;


namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI]
  public static class OptionExts {
    public static Option<B> flatMapUnity<A, B>(this Option<A> opt, Func<A, B> func) where B : class =>
      opt.isSome ? F.opt(func(opt.__unsafeGet)) : F.none<B>();
  }
}