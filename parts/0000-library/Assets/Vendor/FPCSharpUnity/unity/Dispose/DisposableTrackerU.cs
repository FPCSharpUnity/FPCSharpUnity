using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Components.dispose;
using FPCSharpUnity.unity.Logger;
using UnityEngine;

namespace FPCSharpUnity.unity.Dispose {
  /// <summary>
  /// Unity-specific <see cref="DisposableTracker"/>s.
  /// </summary>
  [PublicAPI, HasLogger] public static partial class DisposableTrackerU {
    /// <summary>
    /// We use a GameObject-based tracker for <see cref="disposeOnExitPlayMode"/> because then we can easily see what it
    /// tracks via the Unity Inspector and also kill the subscriptions just by destroying the GameObject. 
    /// </summary>
    static GameObject _disposeOnExitPlayModeGameObject;
    
    static IDisposableTracker _disposeOnExitPlayModeGame {
      get {
        if (Application.isPlaying) {
          var go = _disposeOnExitPlayModeGameObject;

          if (go) {
            return go.asDisposableTracker();
          }
          else {
            // If there is no GameObject, instantiate one.
            go = _disposeOnExitPlayModeGameObject =
              new GameObject($"{nameof(DisposableTrackerU)}.{nameof(disposeOnExitPlayMode)}");
            Object.DontDestroyOnLoad(go);
            var tracker = go.asDisposableTracker();
            // Clean up the reference when GameObject is destroyed.
            tracker.track(() => _disposeOnExitPlayModeGameObject = null);
            return tracker;
          }
        }
        else {
          // It does not make sense to subscribe if we're not playing.
          log.error(
            $"Subscription using {nameof(DisposableTrackerU)}.{nameof(disposeOnExitPlayMode)} while not in play mode. "
            + $"This indicates a bug! Using {nameof(NoOpDisposableTracker)} for now."
          );
          return NoOpDisposableTracker.instance;
        }
      }
    }

    /// <summary>
    /// Use this in methods with <see cref="UnityEngine.RuntimeInitializeOnLoadMethodAttribute"/> instead of
    /// <see cref="NoOpDisposableTracker"/> to dispose properly in editor.
    /// </summary>
    public static IInspectableTracker disposeOnExitPlayMode => _disposeOnExitPlayModeGame;
  }
}