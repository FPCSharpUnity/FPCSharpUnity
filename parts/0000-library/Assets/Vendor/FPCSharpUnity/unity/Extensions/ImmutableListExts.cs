using System.Collections.Immutable;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static class ImmutableListExts {
    public static ImmutableList<A> Add<A>(
      this ImmutableList<A> list, Option<A> maybeA
    ) => maybeA.isSome ? list.Add(maybeA.__unsafeGet) : list;
  }
}