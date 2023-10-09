using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Threads; 

/// <summary>
/// Helper class to queue things from other threads to be ran on the main thread.
/// </summary>
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public static class OnMainThread {
[PublicAPI] 
  static readonly Queue<(Action action, CallerData callerData)> actions = new();
  static Thread mainThread;
    
  public static bool isMainThread {
    get {
      if (mainThread == null) {
        // can't use `Log.d` here, because it calls `isMainThread` and we get infinite recursion
        Debug.LogError(
          $"{nameof(OnMainThread)}.{nameof(isMainThread)} does not know which thread is main!"
        );
      }
      return Thread.CurrentThread == mainThread;
    }
  }

#if UNITY_EDITOR
  // InitializeOnLoad runs before InitializeOnLoadMethod
  static OnMainThread() {
    initMainThreadVariable();
    initOnUpdate();
  }
#endif

  // According to our tests and https://uninomicon.com/runtimeinitializeonload `SubsystemRegistration` is the earliest
  // when Unity runs the code from a main thread:
  //
  //   SubsystemRegistration: id = 1, name =
  //   UnityCallbacks:SubsystemRegistration () (at Assets/UnityCallbacks.cs:35)
  //
  //   AfterAssembliesLoaded: id = 1, name =
  //   UnityCallbacks:AfterAssembliesLoaded () (at Assets/UnityCallbacks.cs:23)
  //
  //   BeforeSplashScreen: id = 1, name =
  //   UnityCallbacks:BeforeSplashScreen () (at Assets/UnityCallbacks.cs:29)
  //
  //   BeforeSceneLoad: id = 1, name =
  //   UnityCallbacks:BeforeSceneLoad () (at Assets/UnityCallbacks.cs:17)
  //
  //   AfterSceneLoad: id = 1, name =
  //   UnityCallbacks:AfterSceneLoad () (at Assets/UnityCallbacks.cs:11)
  //
  // We determined this with the following code:
  //   var t = Thread.CurrentThread;
  //   Debug.Log($"AfterSceneLoad: id = {t.ManagedThreadId}, name = {t.Name}");
  //
  // Therefore we use it to initialize the `mainThread` variable.
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void initMainThreadVariable() {
    // Can't use static constructor, because it may be called from a different thread
    // init will always be called fom a main thread
    var mainThread = OnMainThread.mainThread = Thread.CurrentThread;
    
    // Can't use `Log.d` here because it calls `isMainThread` and we want to be safe.
    Debug.Log(
      $"{Macros.classAndMethodNameShort}: initialized {nameof(mainThread)} to thread id={mainThread.ManagedThreadId}"
    );
  }
  
  // Initialize the main thread scheduler a bit later to make sure everything is properly set up and `ASync.EveryFrame`
  // won't die.
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void initOnUpdate() {
    if (Application.isPlaying) {
      // In players isPlaying is always true.
      ASync.EveryFrame(onUpdate);
    }
#if UNITY_EDITOR
    else {
      EditorApplication.update += () => onUpdate();
    }
#endif
    
    // Can't use `Log.d` here because it calls `isMainThread` and we want to be safe.
    Debug.Log($"{Macros.classAndMethodNameShort}: initialized main thread dispatcher.");
  }

  public static readonly RunAction runAction = action => run(action);

  /// <summary>
  /// Run the given action in the main thread.
  /// </summary>
  public static void run(
    Action action, bool runNowIfOnMainThread = true, [Implicit] CallerData callerData = default
  ) {
    if (isMainThread && runNowIfOnMainThread) {
      try { action(); }
      catch (Exception e) { Log.d.error(e); }
    }
    else lock (actions) { actions.Enqueue((action, callerData)); }
  }

  /// <summary>
  /// Run the given function in the main thread and return a <see cref="Future{A}"/> that completes with the result.
  /// </summary>
  public static Future<A> runFuture<A>(
    Func<A> func, bool runNowIfOnMainThread = true, [Implicit] CallerData callerData = default
  ) {
    if (isMainThread && runNowIfOnMainThread) {
      try {
        return Future.successful(func());
      }
      catch (Exception e) {
        Log.d.error(e);
        return Future<A>.unfulfilled;
      }
    }
    else {
      var future = Future.async<A>(out var promise);
        
      void act() {
        var a = func();
        promise.complete(a);
      }
        
      lock (actions) { actions.Enqueue((act, callerData)); }

      return future;
    }
  }

  static bool onUpdate() {
    while (true) {
      Action current;
      CallerData callerData;
      lock (actions) {
        if (actions.Count == 0) {
          break;
        }
        (current, callerData) = actions.Dequeue();
      }
      try { current(); }
      catch (Exception e) { Log.d.error(e, callerData: callerData); }
    }
    return true;
  }

  /// <summary>
  /// Converts <see cref="Task{A}"/> to <see cref="Future{A}"/> continuing the execution on Unity main thread. 
  /// </summary>
  public static Future<Either<TaskFailed, A>> toFutureOnUnityMainThread<A>(
    this Task<A> task, [Implicit] ILog log=default
  ) => 
    task.toFuture(runAction);
}