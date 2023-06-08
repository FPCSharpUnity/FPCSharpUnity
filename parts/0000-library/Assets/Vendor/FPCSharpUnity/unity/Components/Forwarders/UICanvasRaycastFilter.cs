#nullable enable
using System;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.Forwarders; 

/// <summary>
/// Allows to assign a delegate function to filter raycasts on a UI game object.
/// <para/>
/// This is useful when you want to make a UI element clickable only in some areas or the behavior you need is not
/// possible with the standard way of toggling `raycast target` flag.
/// </summary>
public class UICanvasRaycastFilter : MonoBehaviour, ICanvasRaycastFilter {
  public Func<Vector2, Camera, bool>? filter;
  
  public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => filter?.Invoke(sp, eventCamera) ?? true;
}

public static class UICanvasRaycastFilterExts {
  /// <summary>
  /// Adds `<see cref="UICanvasRaycastFilter"/>` on the game object and assigns the given filter function to it.
  /// </summary>
  /// <param name="go"></param>
  /// <param name="filter">Should return `true` if the raycast is valid.</param>
  /// <param name="log">Used to log an error.</param>
  public static void addUiRaycastFilter(
    this GameObject go, Func<Vector2, Camera, bool> filter, [Implicit] ILog log = default!
  ) {
    var comp = go.EnsureComponent<UICanvasRaycastFilter>();
    if (comp.filter == null) comp.filter = filter;
    else {
      log.error($"Trying to initialize a `{nameof(UICanvasRaycastFilter)}` on `{go}` but it already has one.");
    }
  }
}