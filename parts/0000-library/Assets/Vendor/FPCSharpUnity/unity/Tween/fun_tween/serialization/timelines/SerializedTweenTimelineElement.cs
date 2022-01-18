using System.Collections.Generic;
using FPCSharpUnity.unity.Components;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.sequences {
  /// <summary>
  /// Everything that can go into <see cref="SerializedTweenTimeline"/>.
  /// </summary>
  public abstract class SerializedTweenTimelineElement : ComponentMonoBehaviour, Invalidatable {
    [PublicAPI] public abstract float duration { get; }
    [PublicAPI] public abstract IEnumerable<TweenTimelineElement> elements { get; }
    public abstract void invalidate();
  }
}