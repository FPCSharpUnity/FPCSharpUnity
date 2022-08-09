using System;
using FPCSharpUnity.unity.Dispose;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using UnityEngine;

namespace FPCSharpUnity.unity.Concurrent {
  [PublicAPI] public static class TimeContextExts {
    public static ITimeContextUnity orDefault(this ITimeContextUnity tc) => tc ?? TimeContextU.DEFAULT;
  }

  public class RealTimeButPauseWhenAdIsShowing : ITimeContextUnity {
    public static readonly RealTimeButPauseWhenAdIsShowing instance = new();

    readonly IRxRef<bool> externalPause;
    float totalSecondsPaused, totalSecondsPassed;
    int lastFrameCalculated;
    bool isPaused;

    /// <summary>
    /// This class calculates realtimeSinceStartup,
    /// but excludes time intervals when an ad is showing or application is paused
    ///
    /// on android - interstitials usually run on a separate activity (application gets paused/resumed automatically)
    /// on IOS and some android ad networks - application does not get paused, so we need to call `setPaused` ourselves
    /// </summary>
    RealTimeButPauseWhenAdIsShowing() {
      var pauseStarted = Time.realtimeSinceStartup;
      externalPause = RxRef.a(false);
      ASync.onAppPause.toRxVal(false).zip(externalPause, F.or2).subscribeWithoutEmit(
        NeverDisposeDisposableTracker.instance,
        paused => {
          isPaused = paused;
          if (paused) {
            pauseStarted = Time.realtimeSinceStartup;
          }
          else {
            var secondsPaused = Time.realtimeSinceStartup - pauseStarted;
            totalSecondsPaused += secondsPaused;
          }
        }
      );
    }

    public float passed { get {
      var curFrame = Time.frameCount;
      if (lastFrameCalculated != curFrame) {
        lastFrameCalculated = curFrame;
        if (!isPaused) totalSecondsPassed = Time.realtimeSinceStartup;
      }
      return totalSecondsPassed - totalSecondsPaused;
    } }

    public void setPaused(bool paused) => externalPause.value = paused;

    public TimeSpan passedSinceStartup => Duration.fromSeconds(passed);
    
    IDisposable ITimeContext.after(TimeSpan duration, Action act, string name) => 
      after(duration, act, name);

    public ICoroutine after(TimeSpan duration, Action act, string name = null) =>
      ASync.WithDelay(duration, act, timeContext: this);
  }

  public static class TimeContextU {
    public static readonly MonoBehaviourTimeContext
      playMode = new MonoBehaviourTimeContext(() => Duration.fromSeconds(Time.time)),
      unscaledTime = new MonoBehaviourTimeContext(() => Duration.fromSeconds(Time.unscaledTime)),
      fixedTime = new MonoBehaviourTimeContext(() => Duration.fromSeconds(Time.fixedTime)),
      realTime = new MonoBehaviourTimeContext(() => Duration.fromSeconds(Time.realtimeSinceStartup));
    
    public static readonly ITimeContextUnity
      DEFAULT = playMode,
      realTimeButPauseWhenAdIsShowing = RealTimeButPauseWhenAdIsShowing.instance;
    
#if UNITY_EDITOR
    public static ITimeContextUnity editor => EditorTimeContext.instance;
#endif
  }

  /// <summary>
  /// Time context that depends on a <see cref="MonoBehaviour"/> to measure time.
  /// </summary>
  public class MonoBehaviourTimeContext : ITimeContextUnity {
    readonly Func<Duration> _passedSinceStartup;
    readonly MonoBehaviour maybeBehaviour;

    public MonoBehaviourTimeContext(Func<Duration> passedSinceStartup, MonoBehaviour behaviour = null) {
      _passedSinceStartup = passedSinceStartup;
      maybeBehaviour = behaviour;
    }

    public MonoBehaviourTimeContext withBehaviour(MonoBehaviour behaviour) =>
      new MonoBehaviourTimeContext(_passedSinceStartup, behaviour);

    public TimeSpan passedSinceStartup => _passedSinceStartup();
    
    IDisposable ITimeContext.after(TimeSpan duration, Action act, string name) => 
      after(duration, act, name);

    public ICoroutine after(TimeSpan duration, Action act, string name) =>
      ASync.WithDelay(duration, act, behaviour: maybeBehaviour, timeContext: this);
  }
}