﻿using System;
using System.Collections.Generic;
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
using UnityEngine;

namespace FPCSharpUnity.unity.Threads {
  /// <summary>
  /// Helper class to queue things from other threads to be ran on the main thread.
  /// </summary>
#if UNITY_EDITOR
  [UnityEditor.InitializeOnLoad]
#endif
  [PublicAPI] 
  public static class OnMainThread {
    static readonly Queue<Action> actions = new Queue<Action>();
    static Thread mainThread;
    public static TaskScheduler mainThreadScheduler;
    
    public static bool isMainThread {
      get {
        if (mainThread == null) {
          // can't use Log.d here, because it calls isMainThread
          Debug.LogError(
            $"{nameof(OnMainThread)}#{nameof(isMainThread)} does not know which thread is main!"
          );
        }
        return Thread.CurrentThread == mainThread;
      }
    }

    // mainThread variable may not be initialized in editor when MonoBehaviour constructor gets called
    public static bool isMainThreadIgnoreUnknown => Thread.CurrentThread == mainThread;
    
#if UNITY_EDITOR
    // InitializeOnLoad runs before InitializeOnLoadMethod
    static OnMainThread() => init();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void init() {
      // Can't use static constructor, because it may be called from a different thread
      // init will always be called fom a main thread
      mainThread = Thread.CurrentThread;
      if (Application.isPlaying) {
        // In players isPlaying is always true.
        ASync.EveryFrame(onUpdate);
      }
#if UNITY_EDITOR
      else {
        UnityEditor.EditorApplication.update += () => onUpdate();
      }
#endif
    }

    public static readonly RunAction runAction = action => run(action);

    /// <summary>
    /// Run the given action in the main thread.
    /// </summary>
    public static void run(Action action, bool runNowIfOnMainThread=true) {
      if (isMainThread && runNowIfOnMainThread) {
        try { action(); }
        catch (Exception e) { Log.d.error(e); }
      }
      else lock (actions) { actions.Enqueue(action); }
    }

    static bool onUpdate() {
      while (true) {
        Action current;
        lock (actions) {
          if (actions.Count == 0) {
            break;
          }
          current = actions.Dequeue();
        }
        try { current(); }
        catch (Exception e) { Log.d.error(e); }
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
}
