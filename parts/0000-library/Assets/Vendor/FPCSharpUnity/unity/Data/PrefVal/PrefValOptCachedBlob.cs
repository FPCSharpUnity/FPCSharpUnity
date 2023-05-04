using FPCSharpUnity.unity.caching;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Data {
  [Record]
  partial class PrefValOptCachedBlob<A> : ICachedBlob<A> {
    readonly PrefVal<Option<A>> backing;

    public bool cached => backing.value.isSome;
    public Option<Try<A>> read() => backing.value.map(F.scs);
    public Try<Unit> store(A data) => backing.store(Some.a(data));
    public Try<Unit> clear() => backing.store(None._);
  }
}