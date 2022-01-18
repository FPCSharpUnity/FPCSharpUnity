using System.Collections;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;

namespace FPCSharpUnity.unity.Extensions {
  public static class FutureExts {
    /// <summary>Allows using <see cref="Future{A}"/> as a Unity coroutine.</summary>
    ///
    /// <see cref="FutureEnumerator{A}"/>
    public static IEnumerator toEnumerator<A>(this Future<A> future) => new FutureEnumerator<A>(future);
  }
}