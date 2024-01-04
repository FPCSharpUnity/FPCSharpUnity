using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Logger;
using JetBrains.Annotations;
using FPCSharpUnity.core.log;
using UnityEngine;

namespace FPCSharpUnity.unity.core.Utilities {
  [PublicAPI, HasLogger] public static partial class ApplicationUtils {

    /// <inheritdoc cref="quit(byte)"/>
    public static void quit() => quit(0);
    
    /// <summary> Whether the application is quitting. </summary>
    public static bool isQuitting => _isQuitting && Application.isPlaying;
    static bool _isQuitting;
    
    /// <summary>
    /// GameObjects can't be instantiated while this is true.
    /// </summary>
    public static bool isQuittingOrNotPlaying => _isQuitting || !Application.isPlaying;
    
    /// <summary>
    /// If this is false, then you should expect game objects to be destroyed and should not access them.
    /// E.g. do not return objects to a pool.
    /// </summary>
    public static bool isPlayingNotQuitting => !isQuittingOrNotPlaying;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void afterAssembliesLoaded() {
      Application.quitting += () => _isQuitting = true;
#if UNITY_EDITOR
      UnityEditor.EditorApplication.playModeStateChanged += state => {
        if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) _isQuitting = false;
      };
#endif
    }
    
    /// <summary>
    /// As <see cref="Application.Quit(int)"/> but works in Unity Editor as well.
    /// <para/>
    /// Range for the exit code differs based on operating systems and APIs used but a byte is a safe bet that should
    /// work on all configurations. 
    /// </summary>
    public static void quit(byte exitCode) {
#if UNITY_EDITOR
      // Application.Quit() does not work in the editor so
      // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
      log.mInfo($"Simulating Application.Quit({s(exitCode)}) in Unity Editor.");
      UnityEditor.EditorApplication.isPlaying = false;
#else
      if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WebGLPlayer) {
        Log.d.mWarn($"Application.Quit({exitCode.echoDs()}) is not supported on WebGL.");
      }
      else {
        UnityEngine.Application.Quit(exitCode);
      }
#endif
    }
  }
}