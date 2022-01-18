using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.caching {
  public class ICacheTestImpl<A> : ICache<A> {
    readonly Dictionary<string, ICachedBlobTestImpl<A>> caches =
      new Dictionary<string, ICachedBlobTestImpl<A>>();

    public ICachedBlob<A> blobFor(string name) =>
      caches.getOrUpdate(name, () => new ICachedBlobTestImpl<A>(name));

    public Try<IEnumerable<PathStr>> files => F.scs(Enumerable.Empty<PathStr>());
  }
}