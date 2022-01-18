using FPCSharpUnity.unity.caching;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Data {
  public static class PrefValExts {
    public static ICachedBlob<A> optToCachedBlob<A>(
      this PrefVal<Option<A>> val
    ) => new PrefValOptCachedBlob<A>(val);
  }
}