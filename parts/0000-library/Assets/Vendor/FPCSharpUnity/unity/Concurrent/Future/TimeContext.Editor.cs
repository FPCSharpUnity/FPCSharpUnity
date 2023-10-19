#if UNITY_EDITOR
using System;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using UnityEditor;

namespace FPCSharpUnity.unity.Concurrent {
  [Singleton, PublicAPI] public sealed partial class EditorTimeContext : ITimeContextUnity {
    [LazyProperty] static ILog log => Log.d.withScope(nameof(EditorTimeContext));
    
    public TimeSpan passedSinceStartup => TimeSpan.FromSeconds(EditorApplication.timeSinceStartup);

    IDisposable ITimeContext.after(TimeSpan duration, Action act, string name) => 
      after(duration, act, name);

    public ICoroutine after(TimeSpan duration, Action act, string name = null) =>
      after(duration, act, LogLevel.VERBOSE, name);

    public ICoroutine after(TimeSpan duration, Action act, LogLevel logLevel = LogLevel.VERBOSE, string name = null) => 
      new EditorCoroutine(duration, act, name ?? "unnamed", logLevel);

    class EditorCoroutine : ICoroutine {
      public event CoroutineFinishedOrStopped onFinish;
      public CoroutineState state { get; private set; } = CoroutineState.Running;
      
      readonly TimeSpan duration;
      readonly Action action;
      readonly double startedAt;
      readonly string name;
      readonly LogLevel logLevel;

      double scheduledAt => startedAt + duration.TotalSeconds;
      
      public EditorCoroutine(TimeSpan duration, Action action, string name, LogLevel logLevel) {
        this.duration = duration;
        this.action = action;
        this.name = name;
        this.logLevel = logLevel;
        startedAt = EditorApplication.timeSinceStartup;
        log.mLog(logLevel, $"Scheduling '{name}' at {scheduledAt}, {startedAt.echo()}");
        
        EditorApplication.update += onUpdate;
      }

      void onUpdate() {
        var now = EditorApplication.timeSinceStartup;
        if (now >= scheduledAt) {
          state = CoroutineState.Finished;
          log.mLog(logLevel, $"Running '{name}' at {now.echo()}, {scheduledAt.echo()}, {startedAt.echo()}");
          action();
          onFinish?.Invoke(finished: true);
          onFinish = null;
          dispose(doLog: false);
        }
      }

      public void Dispose() {
        if (!state.isRunning()) return;
        dispose(doLog: true);
      }

      void dispose(bool doLog) {
        if (doLog) {
          log.mLog(logLevel, $"Disposing '{name}' scheduled at {scheduledAt}, {startedAt.echo()}");
        }
        EditorApplication.update -= onUpdate;
        state = CoroutineState.Stopped;
        onFinish?.Invoke(finished: false);
        onFinish = null;
      }

      public void Reset() {}
      public object Current => null;
      public bool MoveNext() => state.isRunning();
    }
  }
}
#endif