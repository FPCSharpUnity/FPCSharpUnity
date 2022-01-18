using System;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Tween.fun_tween {
  public class TweenCallback : TweenTimelineElement {
    public struct Event {
      [PublicAPI] public readonly bool playingForwards;

      public Event(bool playingForwards) { this.playingForwards = playingForwards; }
    }

    [PublicAPI] public readonly Action<Event> callback;

    public TweenCallback(Action<Event> callback) { this.callback = callback; }

    public float duration => 0;
    public void setRelativeTimePassed(
      float previousTimePassed, float timePassed, bool playingForwards, bool applyEffectsForRelativeTweens, 
      bool exitTween, bool isReset
    ) => 
      callback(new Event(playingForwards));

    public bool asApplyStateAt(out IApplyStateAt applyStateAt) {
      applyStateAt = default; 
      return false;
    }
    
    public static TweenCallback a(Action<Event> callback) => new TweenCallback(callback);

    public static TweenCallback forwards(Action callback) => new TweenCallback(evt => {
      if (evt.playingForwards) callback();
    });
  }
}