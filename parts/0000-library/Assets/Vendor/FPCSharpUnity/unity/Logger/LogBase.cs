using System;
using System.Threading;
using FPCSharpUnity.unity.Threads;
using FPCSharpUnity.core.log;
using UnityEngine;

namespace FPCSharpUnity.unity.Logger {
  public abstract class LogBase : BaseLog {
    // Can't use Unity time, because it is not thread safe
    static readonly DateTime initAt = DateTime.Now;

    protected LogBase() => level = Log.defaultLogLevel;

    protected override void logInternal(LogLevel l, LogEntry entry) {
      logInner(l, entry.withMessage(line(l.ToString(), entry.message)));
    }

    protected override void pushToMessageLogged(LogEvent e) {
      if (OnMainThread.isMainThread) base.pushToMessageLogged(e);
      else {
        // extracted method to avoid closure allocation when running on main thread
        logOnMainThread(e);
      }
    }

    void logOnMainThread(LogEvent logEvent) => OnMainThread.run(() => base.pushToMessageLogged(logEvent));

    protected abstract void logInner(LogLevel l, LogEntry entry);

    static string line(string level, object o) => 
      $"[{(DateTime.Now - initAt).TotalSeconds:F3}|{thread}|{frame}|{level}]> {o}";

    static string thread => (OnMainThread.isMainThread ? "Tm" : "T") + Thread.CurrentThread.ManagedThreadId;
    static string frame => (OnMainThread.isMainThread ? "f" + Time.frameCount : "f-");
  }
}