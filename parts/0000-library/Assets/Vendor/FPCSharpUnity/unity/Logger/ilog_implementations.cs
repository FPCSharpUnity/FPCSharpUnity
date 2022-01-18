using System;
using FPCSharpUnity.unity.Concurrent;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.typeclasses;

namespace FPCSharpUnity.unity.Logger {
  /**
   * Useful for logging from inside Application.logMessageReceivedThreaded, because
   * log calls are silently ignored from inside the handlers. Just make sure not to
   * get into an endless loop.
   **/
  [PublicAPI] public sealed class DeferToMainThreadLog : ILog {
    readonly ILog backing;

    public DeferToMainThreadLog(ILog backing) { this.backing = backing; }

    public LogLevel level {
      get => backing.level;
      set => backing.level = value;
    }

    public bool willLog(LogLevel l) => backing.willLog(l);
    public void log(LogLevel l, LogEntry entry) => defer(() => backing.log(l, entry));
    static void defer(Action a) => ASync.OnMainThread(a, runNowIfOnMainThread: false);

    public event ILogMessageLogged messageLogged {
      add { backing.messageLogged += value; }
      remove { backing.messageLogged -= value; }
    }
  }

  /// <summary>
  /// Useful for batch mode to log to the log file without the stacktraces.
  /// </summary>
  [PublicAPI, Singleton] public partial class ConsoleLog : LogBase {
    protected override void logInner(LogLevel l, LogEntry entry) => Console.WriteLine(Str.s(entry));
  }

  [PublicAPI, Singleton] public partial class NoOpLog : LogBase {
    protected override void logInner(LogLevel l, LogEntry entry) {}
  }
}