using System.Collections.Generic;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Extensions {
  public static class HashSetExts {
    [PublicAPI]
    public static Option<A> headOption<A>(this HashSet<A> enumerable) {
      foreach (var a in enumerable)
        return Some.a(a);
      return F.none<A>();
    }
  }
}