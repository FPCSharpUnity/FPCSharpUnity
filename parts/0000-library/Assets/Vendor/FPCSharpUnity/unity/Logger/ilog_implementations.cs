using System;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Concurrent;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.typeclasses;

namespace FPCSharpUnity.unity.Logger {
  /**
   * Useful for logging from inside Application.logMessageReceivedThreaded, because
   * log calls are silently ignored from inside the handlers. Just make sure not to
   * get into an endless loop.
   **/
  [PublicAPI] public sealed class DeferToMainThreadLog : ILog {
    readonly ILog backing;
    public Option<RegisterToLogRegistry> registerToRegistry => backing.registerToRegistry;

    public DeferToMainThreadLog(ILog backing) { this.backing = backing; }

    public LogLevel level {
      get => backing.level;
      set => backing.level = value;
    }

    public bool willLog(LogLevel l) => backing.willLog(l);
    public void logRaw(LogLevel l, LogEntry entry) => defer(() => backing.log(l, entry));
    static void defer(Action a) => ASync.OnMainThread(a, runNowIfOnMainThread: false);

    public event ILogMessageLogged messageLogged {
      add { backing.messageLogged += value; }
      remove { backing.messageLogged -= value; }
    }
  }

  /// <summary>
  /// Useful for batch mode to log to the log file without the stack traces.
  /// </summary>
  [PublicAPI] public partial class ConsoleLog : LogBase {
    public override Option<RegisterToLogRegistry> registerToRegistry { get; }

    public ConsoleLog(Option<RegisterToLogRegistry> registerToRegistry) {
      this.registerToRegistry = registerToRegistry;
    }

    protected override void logInner(LogLevel l, LogEntry entry) => Console.WriteLine(Str.s(entry));
  }

  [PublicAPI, Singleton] public partial class NoOpLog : LogBase {
    public override Option<RegisterToLogRegistry> registerToRegistry => None._;
    protected override void logInner(LogLevel l, LogEntry entry) {}
  }
}