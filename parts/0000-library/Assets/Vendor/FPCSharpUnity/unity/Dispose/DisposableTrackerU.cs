using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.log;
using Log = FPCSharpUnity.unity.Logger.Log;

namespace FPCSharpUnity.unity.Dispose {
  [PublicAPI] public static class DisposableTrackerU {
    [LazyProperty, Implicit] static ILog log => Log.d.withScope(nameof(DisposableTrackerU));

    /// <summary>
    /// Use this in methods with RuntimeInitializeOnLoadMethod instead of NoOpDisposableTracker to dispose properly
    /// in editor
    /// </summary>
    [LazyProperty] static IDisposableTracker _disposeOnExitPlayMode => new DisposableTracker(log);
    public static IInspectableTracker disposeOnExitPlayMode => _disposeOnExitPlayMode;
    
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void init() {
      UnityEditor.EditorApplication.playModeStateChanged += change => {
        if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
          _disposeOnExitPlayMode.Dispose();
        }
      };
    }
#endif
  }
}