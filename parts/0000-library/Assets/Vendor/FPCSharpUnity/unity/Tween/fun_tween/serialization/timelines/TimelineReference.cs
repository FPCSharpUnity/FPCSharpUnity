﻿using System.Collections.Generic;

using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.sequences {
  /// <summary>
  /// <see cref="TweenTimeline"/> as a <see cref="ComponentMonoBehaviour"/>.
  /// </summary>
  [AddComponentMenu("")]
  public partial class TimelineReference : SerializedTweenTimelineElement {
    #region Unity Serialized Fields
#pragma warning disable 649
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [SerializeField, PublicAccessor, NotNull] FunTweenTimeline _timeline;
    // ReSharper restore FieldCanBeMadeReadOnly.Local
#pragma warning restore 649
    #endregion
    IEnumerable<TweenTimelineElement> _elements;

    public override float duration => _timeline.timeline.timeline.duration;
    public override IEnumerable<TweenTimelineElement> elements => 
      _elements ?? (_elements = _timeline.timeline.timeline.yield<TweenTimelineElement>());

    public override void invalidate() {
      _elements = null;
      _timeline.invalidate();
    }
  }
}