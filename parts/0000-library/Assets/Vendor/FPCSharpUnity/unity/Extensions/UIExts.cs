﻿using System;
using FPCSharpUnity.unity.Components;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI] public static partial class UIExts {
    public static IRxObservable<Unit> uiClick(this UIBehaviour elem) => elem.gameObject.uiClick();
    public static IRxObservable<Unit> uiClick(this GameObject go) => go.EnsureComponent<UIClickForwarder>().onClick;
    public static IRxObservable<PointerEventData> uiDown(this GameObject go) => go.EnsureComponent<UIDownUpForwarder>().onDown;
    public static IRxObservable<UIDownUpForwarder.OnUpData> uiUp(this GameObject go) => go.EnsureComponent<UIDownUpForwarder>().onUp;
    
    /// <inheritdoc cref="UIPointerEnterExitForwarder.onHover"/>
    public static IRxObservable<Unit> onHover(this GameObject go) => 
      go.EnsureComponent<UIPointerEnterExitForwarder>().onHover;

    /// <summary>
    /// Returns the <see cref="TimeSpan"/> (which is calculated from <see cref="ITimeContextUnity"/>) between the down
    /// event and up event.
    /// </summary>
    public static IRxObservable<UIDownUpResult> uiDownUp(this GameObject go, ITimeContextUnity timeContext) {
      TimeSpan downMapper(PointerEventData pointerEventData) => timeContext.passedSinceStartup;
      (TimeSpan at, UIDownUpForwarder.OnUpData data) upMapper(UIDownUpForwarder.OnUpData pointerEventData) =>
        (timeContext.passedSinceStartup, pointerEventData);

      var downAt = go.uiDown().map(downMapper);
      var upAt = go.uiUp().map(upMapper);
      return downAt
        .zipObservable(upAt, (downAt, up) =>
          // We need to filter this to prevent an event firing in the case of UP -> DOWN event sequence. 
          up.at >= downAt ? Some.a(new UIDownUpResult(up.at - downAt, up.data)) : None._
        )
        .collect(_ => _);
    }
  }

  [Record] public readonly partial struct UIDownUpResult {
    public readonly TimeSpan pressedDuration;
    public readonly UIDownUpForwarder.OnUpData upData;
  }
}