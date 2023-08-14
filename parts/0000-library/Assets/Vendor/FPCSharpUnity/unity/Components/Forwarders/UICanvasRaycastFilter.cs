#nullable enable
using System;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders; 

/// <summary>
/// Allows to assign a delegate function to filter raycasts on a UI game object.
/// <para/>
/// This is useful when you want to make a UI element clickable only in some areas or the behavior you need is not
/// possible with the standard way of toggling `raycast target` flag.
/// </summary>
[PublicAPI] public class UICanvasRaycastFilter : MonoBehaviour, ICanvasRaycastFilter {
  /// <returns>`true` if the raycast is valid.</returns>
  public delegate bool Filter(Vector2 screenSpacePosition, Camera eventCamera);
  
  public Filter? filter;
  
  public bool IsRaycastLocationValid(Vector2 screenSpacePosition, Camera eventCamera) => 
    filter?.Invoke(screenSpacePosition, eventCamera) ?? true;
}

[PublicAPI] public static class UICanvasRaycastFilterExts {
  /// <summary>
  /// Ensures `<see cref="UICanvasRaycastFilter"/>` exists on the game object and assigns the given filter function to
  /// it.
  /// <para/>
  /// Returns `Left` if a filter is already set.
  /// </summary>
  public static Either<LogEntry, Unit> trySetUiRaycastFilter(
    this GameObject go, ITracker tracker, UICanvasRaycastFilter.Filter filter, 
    [Implicit] CallerData callerData = default
  ) {
    var comp = go.EnsureComponent<UICanvasRaycastFilter>();
    var maybeExistingFilter = comp.filter;
    if (maybeExistingFilter == null) {
      comp.filter = filter;
      tracker.track(() => comp.filter = null);
      return Unit._;
    }
    else {
      return LogEntry.simple(
        $"Trying to set a `{nameof(UICanvasRaycastFilter)}.{nameof(UICanvasRaycastFilter.Filter)}` on `{go}` but "
        + $"it already has one.",
        context: go
      );
    }
  }
  
  /// <summary>
  /// Ensures `<see cref="UICanvasRaycastFilter"/>` exists on the game object and assigns the given filter function to
  /// it.
  /// <para/>
  /// Overrides the existing filter if it is already set.
  /// </summary>
  public static void setUiRaycastFilter(
    this GameObject go, ITracker tracker, UICanvasRaycastFilter.Filter filter, 
    [Implicit] CallerData callerData = default
  ) {
    var component = go.EnsureComponent<UICanvasRaycastFilter>();
    component.filter = filter;
    tracker.track(() => component.filter = null);
  }

  /// <summary>
  /// If `<see cref="UICanvasRaycastFilter"/>` exists on the game object and clears filter on it.
  /// </summary>
  public static void clearUiRaycastFilter(this GameObject go) {
    if (go.TryGetComponent(out UICanvasRaycastFilter component)) {
      component.filter = null;
    }
  }
}