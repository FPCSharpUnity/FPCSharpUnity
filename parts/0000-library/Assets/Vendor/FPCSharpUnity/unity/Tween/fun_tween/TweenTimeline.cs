﻿using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween {
  /// <summary>
  /// Anything that can be added into a <see cref="ITweenTimeline"/>.
  /// </summary>
  public interface TweenTimelineElement {
    float duration { get; }

    /// <summary>
    /// Sets how much time has passed, relative to elements duration.
    /// 
    /// Must be [0, duration].
    /// </summary>
    /// 
    /// <param name="previousTimePassed"></param>
    /// <param name="timePassed"></param>
    /// <param name="playingForwards"></param>
    /// <param name="applyEffectsForRelativeTweens">
    /// Should we run effects for relative tweens when setting the time passed?
    /// 
    /// Usually you want to run them, but one case when you do not want effects to happen
    /// is rewinding - you want for the object to stay in place even though the logical time
    /// of the sequence changes to 0 or total duration. 
    /// </param>
    /// <param name="exitTween">
    /// Did the tween timeline position exit this tween?
    /// Useful for implementing tweens that do something on exit, but we do not want to trigger exit
    /// when tween ends at the same time as the whole timeline.
    /// ToggleTweenBase implements this feature.
    /// </param>
    /// <param name="isReset">
    /// Is set when we want to reset all elements to start/end. Also this flag is used to determine whether to invoke
    /// some of the callback events. (ex. it doesn't make sense to invoke 'play sound' callback on tween reset)
    /// </param>
    void setRelativeTimePassed(
      float previousTimePassed, float timePassed, bool playingForwards, 
      bool applyEffectsForRelativeTweens, bool exitTween, bool isReset
    );

    // Not an option for performance.
    bool asApplyStateAt(out IApplyStateAt applyStateAt);
  }

  public interface IApplyStateAt {
    /// <summary>
    /// Applies absolute <see cref="Tweener{A,T}"/>s at a given time.
    ///
    /// Useful to force object states into those, which would be if tween was playing at
    /// some time, for example - 0s.
    /// </summary>
    void applyStateAt(float time);
  }

  /// <summary>
  /// <see cref="TweenTimelineElement"/>s arranged in time.
  /// </summary>
  public interface ITweenTimeline : TweenTimelineElement, IApplyStateAt {
    /// <summary>Calls <see cref="setTimePassed"/> applying effects for relative tweens.</summary>
    float timePassed { get; set; }
    
    /// <see cref="TweenTimelineElement.setRelativeTimePassed"/>
    void setTimePassed(float timePassed, bool applyEffectsForRelativeTweens);
  }

  public class TweenTimeline : ITweenTimeline {
    public readonly struct Effect {
      public readonly float startsAt, endsAt;
      public readonly TweenTimelineElement element;

      public readonly float duration;
      public float relativize(float timePassed) => timePassed - startsAt;

      public Effect(float startsAt, float endsAt, TweenTimelineElement element) {
        this.startsAt = startsAt;
        this.endsAt = endsAt;
        this.element = element;
        duration = endsAt - startsAt;
      }
    }

    /// <summary>Duration in seconds</summary>
    public float duration { get; }
    public readonly Effect[] effects;

    bool lastDirectionWasForwards;

    float _timePassed;
    
    /// <summary>
    /// This value is automatically clamped to [0; duration].
    /// It is optimized to do nothing when the time did not change.
    /// </summary>
    public float timePassed {
      get => _timePassed;
      set => setTimePassed(value, true);
    }

    public void setTimePassed(float value, bool applyEffectsForRelativeTweens) {
      // ReSharper disable once CompareOfFloatsByEqualityOperator
      var newTimePassed = value.clamp(0, duration);
      if (_timePassed == newTimePassed) return;
      var playingForwards = newTimePassed >= _timePassed; 
        
      setRelativeTimePassed(
        previousTimePassed: _timePassed, timePassed: newTimePassed, playingForwards: playingForwards, 
        applyEffectsForRelativeTweens: applyEffectsForRelativeTweens, exitTween: false, isReset: false
      );
    }

    public void setTimePassedToStart() => timePassed = 0;
    public void setTimePassedToEnd() => timePassed = duration;
    public void setTimePassedPercentage(float percentage) => timePassed = duration * percentage;
    public void setTimePassedPercentage(Percentage percentage) => timePassed = duration * percentage.value;

    TweenTimeline(float duration, Effect[] effects) {
      this.duration = duration;
      this.effects = effects;
    }

    // optimized for minimal allocations
    [PublicAPI]
    public static TweenTimeline single(TweenTimelineElement timeline, float delay = 0) =>
      new TweenTimeline(
        timeline.duration + delay,
        new []{ new Effect(delay, timeline.duration + delay, timeline) }
      );
    
    /// <summary>
    /// We need to create new timeline each time because we have internal mutable state.
    /// </summary>
    public static TweenTimeline empty() => new TweenTimeline(duration: 0, effects: Array.Empty<Effect>());

    public void setRelativeTimePassed(
      float previousTimePassed, float timePassed, bool playingForwards, bool applyEffectsForRelativeTweens, 
      bool exitTween, bool isReset
    ) {
      _timePassed = Mathf.Clamp(timePassed, 0, duration);

      // ReSharper disable once CompareOfFloatsByEqualityOperator
      if (previousTimePassed == _timePassed && playingForwards == lastDirectionWasForwards) return;

      var directionChanged = playingForwards != lastDirectionWasForwards;

      if (playingForwards) {
        foreach (var effect in effects) {
          if (timePassed >= effect.startsAt && previousTimePassed <= effect.endsAt) {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (previousTimePassed == effect.endsAt) {
              if (directionChanged)
                effect.element.setRelativeTimePassed(
                  effect.duration, effect.duration, true, applyEffectsForRelativeTweens,
                  // We might want to use here `timePassed > effect.endsAt || exitTween` here,
                  // but that would cause undesired results when nesting different tween timelines.
                  exitTween: timePassed > effect.endsAt,
                  isReset: isReset
                );
            }
            else {
              float t(float x) => x <= effect.endsAt ? effect.relativize(x) : effect.duration;
              effect.element.setRelativeTimePassed(
                t(previousTimePassed), t(timePassed), true, applyEffectsForRelativeTweens,
                exitTween: timePassed > effect.endsAt, isReset: isReset
              );
            }
          }
        }
      }
      else {
        for (var idx = effects.Length - 1; idx >= 0; idx--) {
          var effect = effects[idx];
          if (timePassed <= effect.endsAt && previousTimePassed >= effect.startsAt) {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (previousTimePassed == effect.startsAt) {
              if (directionChanged) effect.element.setRelativeTimePassed(
                0, 0, false, applyEffectsForRelativeTweens, 
                exitTween: timePassed < effect.startsAt,
                isReset: isReset
              );
            }
            else {
              float t(float x) => x >= effect.startsAt ? effect.relativize(x) : 0;
              effect.element.setRelativeTimePassed(
                t(previousTimePassed), t(timePassed), false, applyEffectsForRelativeTweens,
                exitTween: timePassed < effect.startsAt,
                isReset: isReset
              );
            }
          }
        }
      }
      lastDirectionWasForwards = playingForwards;
    }

    public bool asApplyStateAt(out IApplyStateAt applyStateAt) {
      applyStateAt = this;
      return true;
    }

    public void applyStateAt(float time) {
      foreach (var effect in effects) {
        if (
          time >= effect.startsAt  
          && time <= effect.endsAt 
          && effect.element.asApplyStateAt(out var stateEffect)
        ) {
          stateEffect.applyStateAt(effect.relativize(time));          
        }
      }
    }
    
    [PublicAPI]
    public class Builder {
      public float totalDuration { get; private set; }
      readonly List<Effect> effects = new List<Effect>();

      public TweenTimeline build() => new TweenTimeline(
        totalDuration,
        effects.OrderBySafe(_ => _.startsAt).ToArray()
      );

      public static Builder create() => new Builder();

      /// <summary>Inserts element into the sequence at specific time.</summary>
      public Builder insert(float at, TweenTimelineElement element) {
        var endsAt = at + element.duration;
        totalDuration = Mathf.Max(totalDuration, endsAt);
        effects.Add(new Effect(at, endsAt, element));
        return this;
      }

      public Builder insert(float at, Action<TweenCallback.Event> callback) =>
        insert(at, new TweenCallback(callback));

      /// <see cref="insert(float,TweenTimelineElement)"/>
      /// <returns>Time when the given element will end.</returns>
      public float insert2(float at, TweenTimelineElement element) {
        insert(at, element);
        return at + element.duration;
      }

      /// <see cref="insert(float,TweenTimelineElement)"/>
      /// <param name="at"></param>
      /// <param name="element"></param>
      /// <param name="elementEndsAt">Time when the given element will end.</param>
      public Builder insert(float at, TweenTimelineElement element, out float elementEndsAt) {
        insert(at, element);
        elementEndsAt = at + element.duration;
        return this;
      }

      public Builder append(TweenTimelineElement element) =>
        insert(totalDuration, element);
      
      public Builder append(Action<TweenCallback.Event> callback) =>
        append(new TweenCallback(callback));

      public Builder append(Option<TweenTimelineElement> element) =>
        element.isSome ? append(element.__unsafeGet) : this;

      public float append2(TweenTimelineElement element) =>
        insert2(totalDuration, element);

      public Builder append(TweenTimelineElement element, out float elementEndsAt) =>
        insert(totalDuration, element, out elementEndsAt);

      /// <summary>
      /// Use this to append delay between append operations.
      /// Or if you want to append delay at the end of the whole tween.
      /// </summary>
      public Builder appendDelay(float delaySeconds) {
        totalDuration += delaySeconds;
        return this;
      }

      /// <summary>
      /// Use this to append delay between append operations.
      /// Or if you want to append delay at the end of the whole tween.
      /// </summary>
      public Builder appendDelay(Duration delay) => appendDelay(delay.seconds);
    }

    [PublicAPI] 
    public static Builder parallelEnumerable(IEnumerable<TweenTimelineElement> elements) {
      var builder = Builder.create();
      foreach (var element in elements)
        builder.insert(0, element);
      return builder;
    }

    [PublicAPI] 
    public static Builder parallel(params TweenTimelineElement[] elements) =>
      parallelEnumerable(elements);

    [PublicAPI] 
    public static Builder sequentialEnumerable(IEnumerable<TweenTimelineElement> elements) {
      var builder = Builder.create();
      foreach (var element in elements)
        builder.append(element);
      return builder;
    }

    [PublicAPI]
    public static Builder sequential(params TweenTimelineElement[] elements) =>
      sequentialEnumerable(elements);

    [PublicAPI]
    public static Builder withGrowingDelay(IEnumerable<TweenTimeline> tweens, Duration delayBetweenEach) {
      var builder = Builder.create();
      var index = 0;
      foreach (var tween in tweens) {
        builder.insert(delayBetweenEach.seconds * index++, tween);
      }
      return builder;
    }
    
    [PublicAPI] public static Builder builder() => Builder.create();
  }

  // TODO: this fires forwards events, when playing from the end. We should fix this.
  class TweenTimelineReversed : ITweenTimeline {
    public readonly ITweenTimeline original;

    public TweenTimelineReversed(ITweenTimeline original) { this.original = original; }

    public float duration => original.duration;

    public void setRelativeTimePassed(
      float previousTimePassed, float timePassed, bool playingForwards, bool applyEffectsForRelativeTweens, 
      bool exitTween, bool isReset
    ) =>
      original.setRelativeTimePassed(
        previousTimePassed: original.duration - previousTimePassed,
        timePassed: original.duration - timePassed, 
        playingForwards: !playingForwards,
        applyEffectsForRelativeTweens: applyEffectsForRelativeTweens,
        exitTween: exitTween, isReset: isReset
      );

    public bool asApplyStateAt(out IApplyStateAt applyStateAt) => original.asApplyStateAt(out applyStateAt);
    public void applyStateAt(float time) => original.applyStateAt(original.duration - time);

    public void setTimePassed(float timePassed, bool applyEffectsForRelativeTweens) =>
      original.setTimePassed(original.duration - timePassed, applyEffectsForRelativeTweens);

    public float timePassed {
      get => original.duration - original.timePassed;
      set => setTimePassed(value, true);
    }
  }

  public static class TweenTimeLineExts {
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public static bool isAtZero(this ITweenTimeline ts) => ts.timePassed == 0;
    public static bool isAtDuration(this ITweenTimeline ts) => ts.timePassed == ts.duration;
    // ReSharper restore CompareOfFloatsByEqualityOperator

    [PublicAPI]
    public static ITweenTimeline reversed(this ITweenTimeline ts) {
      if (ts is TweenTimelineReversed r)
        return r.original;
      return new TweenTimelineReversed(ts);
    }

    [PublicAPI]
    public static TweenTimeline.Builder singleBuilder(this TweenTimelineElement element) {
      var builder = TweenTimeline.Builder.create();
      builder.append(element);
      return builder;
    }

    public static void update(this ITweenTimeline element, float deltaTime) {
      // ReSharper disable once CompareOfFloatsByEqualityOperator
      if (deltaTime == 0) return;

      element.timePassed += deltaTime;
    }
    
    public static void applyAtStart(this ITweenTimeline tt) => tt.applyStateAt(0);
    public static void applyAtEnd(this ITweenTimeline tt) => tt.applyStateAt(tt.duration);
    
    /// <summary>
    /// If we have timeline element that starts moving position at 1 second,
    /// we will get a stutter if the object is placed at other position
    ///
    /// Call this method to set all tween targets at start position.
    /// </summary>
    public static void resetAllElementsToStart(this ITweenTimeline tt) {
      tt.setRelativeTimePassed(
        previousTimePassed: tt.duration, 
        timePassed: 0,
        playingForwards: false,
        applyEffectsForRelativeTweens: false,
        exitTween: false,
        isReset: true
      );
    }

    /// <summary>
    /// Call this method to set all tween targets at end position.
    /// </summary>
    public static void resetAllElementsToEnd(this ITweenTimeline tt) {
      tt.setRelativeTimePassed(
        previousTimePassed: 0,
        timePassed: tt.duration,
        playingForwards: true,
        applyEffectsForRelativeTweens: false,
        exitTween: false,
        isReset: true
      );
    }

    [PublicAPI] 
    public static TweenTimeline single(this TweenTimelineElement element) =>
      TweenTimeline.single(element);
  }
}