using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.caching {
  public class ICachedBlobTestImpl<A> : ICachedBlob<A> {
    public readonly string name;

    public ICachedBlobTestImpl() : this("not set") { }
    public ICachedBlobTestImpl(string name) { this.name = name; }

    public Option<A> blob = None._;

    public bool cached => blob.isSome;

    public Option<Try<A>> read() => blob.mapM(F.scs);

    public Try<Unit> store(A data) {
      blob = data.some();
      return F.scs(F.unit);
    }

    public Try<Unit> clear() {
      blob = None._;
      return F.scs(F.unit);
    }
  }
}