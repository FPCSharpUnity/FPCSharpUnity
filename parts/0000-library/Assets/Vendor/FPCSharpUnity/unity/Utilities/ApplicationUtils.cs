using FPCSharpUnity.core.dispose;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.log;
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.core.Utilities {
  [PublicAPI] public static class ApplicationUtils {
    [LazyProperty] static ILog log => Log.d.withScope(nameof(ApplicationUtils));

    /// <inheritdoc cref="quit(byte)"/>
    public static void quit() => quit(0);
    
    /// <summary> Whether the application is quitting. </summary>
    public static bool isQuitting => _isQuitting && Application.isPlaying;
    static bool _isQuitting;
    static IDisposableTracker isQuittingTracker = new DisposableTracker(log);
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void afterAssembliesLoaded() {
      _isQuitting = false;
      isQuittingTracker.Dispose();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void beforeSceneLoad() {
      _isQuitting = false;
      isQuittingTracker.Dispose();
      Application.quitting += onQuit;
      Application.quitting += onQuit;
      isQuittingTracker.track(() => Application.quitting -= onQuit);
      
      static void onQuit() => _isQuitting = true;
    }
    
    /// <summary>
    /// As <see cref="Application.Quit(int)"/> but works in Unity Editor as well.
    ///
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
      UnityEngine.Application.Quit(exitCode);
#endif
    }
  }
}