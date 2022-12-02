using ExhaustiveMatching;
using FPCSharpUnity.core.reactive;

namespace FPCSharpUnity.unity.Utilities {
#if UNITY_EDITOR
  [UnityEditor.InitializeOnLoad]
#endif
  public static class PlayStateUtils {
    /// <summary>
    /// Becomes `false` when we exit the play mode and also when we're about to exit play mode.
    /// <para/>
    /// Works in Editor (by using <see cref="UnityEditor.EditorApplication.playModeStateChanged"/>) and Player (in player
    /// this always returns true).
    /// </summary>
    public static readonly IRxVal<bool> isPlayingAndNotExitingPlayMode;
  
    static PlayStateUtils() {
      isPlayingAndNotExitingPlayMode =
#if UNITY_EDITOR
        createIsPlayingAndNotExitingPlayModeForEditor();
#else
      // If we're not in editor, then we're just always playing.
      RxVal.cached(true)
#endif
    
#if UNITY_EDITOR
      static IRxVal<bool> createIsPlayingAndNotExitingPlayModeForEditor() {
        var rx = RxRef.a(UnityEditor.EditorApplication.isPlaying);
        UnityEditor.EditorApplication.playModeStateChanged += change => rx.value = change switch {
          UnityEditor.PlayModeStateChange.EnteredEditMode => false,
          UnityEditor.PlayModeStateChange.ExitingEditMode => false,
          UnityEditor.PlayModeStateChange.EnteredPlayMode => true,
          UnityEditor.PlayModeStateChange.ExitingPlayMode => false,
          _ => throw ExhaustiveMatch.Failed(change)
        };
        return rx;
      }
#endif
      ;
    }
  }
}