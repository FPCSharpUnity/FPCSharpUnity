﻿using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ExhaustiveMatching;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using Sirenix.OdinInspector;
using UnityEngine;


namespace FPCSharpUnity.unity.Tween.fun_tween {
  public enum TweenTime : byte {
    OnUpdate, OnUpdateUnscaled, OnLateUpdate, OnLateUpdateUnscaled, OnFixedUpdate
  }

  /// <summary>
  /// Manages a sequence, calling its <see cref="TweenTimeline.setRelativeTimePassed"/> method for you on
  /// your specified terms (for example loop 3 times, run on fixed update).
  /// </summary>
  public partial class TweenManager : IDisposable {
    [Serializable, Record(GenerateToString = false), InlineProperty, PublicAPI]
    public partial struct Loop {
      public enum Mode : byte { Normal, YoYo }

      public const uint
        TIMES_FOREVER = 0,
        TIMES_SINGLE = 1;

      #region Unity Serialized Fields

#pragma warning disable 649
      // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
      [SerializeField, PublicAccessor, HorizontalGroup, HideLabel, InfoBox("0 means loop forever")] uint _times_;
      [SerializeField, PublicAccessor, HorizontalGroup, HideLabel] Mode _mode;
      // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

      #endregion

      public override string ToString() {
        var timesS = _times_ == TIMES_FOREVER ? "forever" : _times_.ToString();
        return $"Loop({_mode} x {timesS})";
      }

      public bool shouldLoop(uint currentIteration) => isForever || currentIteration < times_ - 1;
      public bool isForever => times_ == TIMES_FOREVER;

      public static Loop forever(Mode mode = Mode.Normal) => new Loop(TIMES_FOREVER, mode);
      public static Loop foreverYoYo => new Loop(TIMES_FOREVER, Mode.YoYo);
      public static Loop single => new Loop(TIMES_SINGLE, Mode.Normal);
      public static Loop singleYoYo => new Loop(2, Mode.YoYo);
      public static Loop times(uint times, Mode mode = Mode.Normal) => new Loop(times, mode);
    }

    [PublicAPI] public readonly ITweenTimeline timeline;
    [PublicAPI] public readonly TweenTime time;

    IDisposableTracker _tracker;
    IDisposableTracker tracker => _tracker ??= new DisposableTracker();

    // These are null intentionally. We try not to create objects if they are not needed.
    ISubject<TweenCallback.Event> __onStartSubject, __onEndSubject;
    IRxRef<bool> __isPlayingRx;

    [PublicAPI] public IRxObservable<TweenCallback.Event> onStart => 
      __onStartSubject ??= new Subject<TweenCallback.Event>();
    [PublicAPI] public IRxObservable<TweenCallback.Event> onEnd =>
      __onEndSubject ??= new Subject<TweenCallback.Event>();
    
    [PublicAPI] public IRxVal<bool> isPlayingRx => __isPlayingRx ??= RxRef.a(isPlaying);

    [PublicAPI] public float timescale = 1;
    [PublicAPI] public bool forwards = true;
    [PublicAPI] public Loop looping;
    [PublicAPI] public uint currentIteration;
    [PublicAPI] public bool isPlaying { get; private set; }
    public readonly string context;
    public readonly Option<Component> maybeParentComponent;
    public readonly TweenManagerLifetime lifetime;
    
    [LazyProperty, Implicit] static ILog log => Log.d.withScope(nameof(TweenManager));

    public TweenManager(
      ITweenTimeline timeline, TweenManagerLifetime lifetime, 
      TweenTime time, Loop looping, GameObject context = null,
      // stops playing the tween when parent component gets destroyed
      // this is a workaround, for missing OnDestroy callback
      Option<Component> maybeParentComponent = default,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) {
      this.lifetime = lifetime;
      this.timeline = timeline;
      this.time = time;
      this.looping = looping;
      this.maybeParentComponent = maybeParentComponent;
      var callerData = 
        new CallerData(memberName: callerMemberName, filePath: callerFilePath, lineNumber: callerLineNumber);
      this.context = 
        context 
        ? $"game object='{fullName(context.transform)}', manager created at {callerData}" 
        : callerData.ToString();

      static string fullName(Transform t) {
        if (t == null) return "null context";
        if (t.parent == null) return t.gameObject.scene.name + "/" + t.name;
        return fullName(t.parent) + "/" + t.name;
      }
    }

    public Option<Exception> update(float deltaTime, bool doLog=true) {
      try {
        updateWithScaledTime(deltaTime * timescale);
        return None._;
      }
      catch (Exception e) {
        if (doLog) log.error(e, context);
        return Some.a(e);
      }
    }

    void updateWithScaledTime(float deltaTime) {
      if (!forwards) deltaTime *= -1;

      // ReSharper disable once CompareOfFloatsByEqualityOperator
      if (deltaTime == 0) return;

      if (
        currentIteration == 0 
        && (forwards && timeline.isAtZero() || !forwards && timeline.isAtDuration())
      ) {
        __onStartSubject?.push(new TweenCallback.Event(forwards));
      }

      var previousTime = timeline.timePassed;
      timeline.update(deltaTime);

      if (forwards && timeline.isAtDuration() || !forwards && timeline.isAtZero()) {
        if (
          looping.shouldLoop(currentIteration) 
          // Avoid entering endless loops.
          && timeline.duration > 0.001f
        ) {
          currentIteration++;
          var unusedTime =
            Math.Abs(previousTime + deltaTime - (forwards ? timeline.duration : 0));
          switch (looping.mode) {
            case Loop.Mode.YoYo:
              reverse();
              break;
            case Loop.Mode.Normal:
              rewindTimePassed(false);
              break;
            default:
              throw ExhaustiveMatch.Failed(looping.mode);
          }
          updateWithScaledTime(unusedTime);
        }
        else {
          __onEndSubject?.push(new TweenCallback.Event(forwards));
          stop();
        }
      }
    }

    [PublicAPI]
    public TweenManager addOnStartCallback(Action<TweenCallback.Event> act) {
      onStart.subscribe(tracker, act);
      return this;
    }

    [PublicAPI]
    public TweenManager addOnEndCallback(Action<TweenCallback.Event> act) {
      onEnd.subscribe(tracker, act);
      return this;
    }

    /// <summary>Plays a tween from the start/end.</summary>
    [PublicAPI]
    public TweenManager play(bool forwards = true) {
      this.forwards = forwards;
      // Rewind should go before resume.
      // Note that rewind uses forwards value.
      rewind();
      return resume();
    }

    /// <summary>Plays a tween from the start at a given position.</summary>
    // TODO: add an option to play backwards (and test it)
    [PublicAPI]
    public TweenManager play(float startTime) {
      forwards = true;
      // Note that rewind uses forwards value.
      rewind();
      resume();
      timeline.timePassed = startTime;
      return this;
    }
    
    /// <summary>Resumes playback from the last position, changing the direction.</summary>
    [PublicAPI]
    public TweenManager resume(bool forwards) {
      this.forwards = forwards;
      return resume();
    }

    /// <summary>Resumes playback from the last position.</summary>
    [PublicAPI]
    public TweenManager resume() {
      isPlaying = true;
      if (__isPlayingRx != null) __isPlayingRx.value = true;
      TweenManagerRunner.instance.add(this);
      return this;
    }

    /// <summary>Stops playback of the tween</summary>
    [PublicAPI]
    public TweenManager stop() {
      __afterTweenStop();
      if (TweenManagerRunner.hasActiveInstance) {
        // TweenManagerRunner.instance gets destroyed when we exit play mode
        // We don't want to create a new instance once that happens
        TweenManagerRunner.instance.remove(this);
      }
      return this;
    }
    
    /// <summary>
    /// Stops playback of the tween and sets all tween targets to start position.
    /// </summary>
    [PublicAPI]
    public TweenManager stopAndResetToStart() {
      stop();
      timeline.resetAllElementsToStart();
      return this;
    }
    
    /// <summary>
    /// Stops playback of the tween and sets all tween targets to end position.
    /// </summary>
    [PublicAPI]
    public TweenManager stopAndResetToEnd() {
      stop();
      timeline.resetAllElementsToEnd();
      return this;
    }

    /// <summary>After stopping tween don't forget to call this! For internal use only!</summary>
    public void __afterTweenStop() {
      isPlaying = false;
      if (__isPlayingRx != null) __isPlayingRx.value = false;
    }

    [PublicAPI]
    public TweenManager reverse() {
      forwards = !forwards;
      return this;
    }

    [PublicAPI]
    public TweenManager rewind(bool applyEffectsForRelativeTweens = false) {
      currentIteration = 0;
      rewindTimePassed(applyEffectsForRelativeTweens);
      return this;
    }

    void rewindTimePassed(bool applyEffectsForRelativeTweens) =>
      timeline.setTimePassed(forwards ? 0 : timeline.duration, applyEffectsForRelativeTweens);

    public void Dispose() {
      stop();
      _tracker?.Dispose();
    }
  }

  public static class TweenManagerExts {
    [PublicAPI]
    public static TweenManager managed(
      this ITweenTimeline timeline, TweenManagerLifetime lifetime, TweenTime time = TweenTime.OnUpdate,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) => new TweenManager(
      timeline, lifetime, time, TweenManager.Loop.single,
      // ReSharper disable ExplicitCallerInfoArgument
      callerFilePath: callerFilePath, callerLineNumber: callerLineNumber, callerMemberName: callerMemberName
      // ReSharper restore ExplicitCallerInfoArgument
    );

    [PublicAPI]
    public static TweenManager managed(
      this ITweenTimeline timeline, TweenManagerLifetime lifetime, 
      TweenManager.Loop looping, TweenTime time = TweenTime.OnUpdate,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) => new TweenManager(
      timeline, lifetime, time, looping,
      // ReSharper disable ExplicitCallerInfoArgument
      callerFilePath: callerFilePath, callerLineNumber: callerLineNumber, callerMemberName: callerMemberName
      // ReSharper restore ExplicitCallerInfoArgument
    );

    [PublicAPI]
    public static TweenManager managed(
      this TweenTimelineElement timeline, TweenManagerLifetime lifetime, 
      TweenTime time = TweenTime.OnUpdate, float delay = 0,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) => timeline.managed(
      lifetime, TweenManager.Loop.single, time, delay,
      // ReSharper disable ExplicitCallerInfoArgument
      callerFilePath: callerFilePath, callerLineNumber: callerLineNumber, callerMemberName: callerMemberName
      // ReSharper restore ExplicitCallerInfoArgument
    );

    [PublicAPI]
    public static TweenManager managed(
      this TweenTimelineElement timeline, TweenManagerLifetime lifetime, 
      TweenManager.Loop looping, TweenTime time = TweenTime.OnUpdate,
      float delay = 0,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) => new TweenManager(
      TweenTimeline.single(timeline, delay), lifetime, time, looping,
      // ReSharper disable ExplicitCallerInfoArgument
      callerFilePath: callerFilePath, callerLineNumber: callerLineNumber, callerMemberName: callerMemberName
      // ReSharper restore ExplicitCallerInfoArgument
    );

    [PublicAPI]
    public static TweenManager managed(
      this TweenTimelineElement timeline, ITracker lifetimeTracker, 
      TweenManager.Loop looping, TweenTime time = TweenTime.OnUpdate,
      float delay = 0,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) {
      var manager = new TweenManager(
        TweenTimeline.single(timeline, delay), TweenManagerLifetime.unbounded, time, looping,
        // ReSharper disable ExplicitCallerInfoArgument
        callerFilePath: callerFilePath, callerLineNumber: callerLineNumber, callerMemberName: callerMemberName
        // ReSharper restore ExplicitCallerInfoArgument
      );
      lifetimeTracker.track(manager);
      return manager;
    }

    [PublicAPI]
    public static IEnumerator onEndEnumerator(
      this TweenManager manager, ITracker tracker
    ) => manager.onEnd.toFuture(tracker).toEnumerator();
  }
}