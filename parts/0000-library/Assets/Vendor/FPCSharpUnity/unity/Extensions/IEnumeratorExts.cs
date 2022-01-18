using System.Collections;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.dispose;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class IEnumeratorExts {
    public static IEnumerator withDelay(this IEnumerator enumeratorNext, float seconds) {
      yield return new WaitForSeconds(seconds);
      yield return enumeratorNext;
    }
    
    public static IEnumerator cancellableDelay(
      Duration duration, IDisposableTracker tracker, TimeScale timeScale = TimeScale.Unity
    ) {
      var shouldStop = false;
      tracker.track(() => shouldStop = true);
      foreach (var _ in new CoroutineInterval(duration, timeScale)) {
        if (shouldStop) break;
        yield return null;
      }
    }    
  }
}