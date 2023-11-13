using System;
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.utils.registry;
using FPCSharpUnity.unity.Threads;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Logger {
  public class UnityLog : LogBase {
    /// <summary>
    /// Prefix to all messages so we could differentiate what comes from
    /// our logging framework in Unity console.
    /// </summary>
    const string MESSAGE_PREFIX = "[FPC#Log]";
    
    public static readonly ProfilerMarker markerConvertUnityMessageToLogEvent = new ("convertUnityMessageToLogEvent");

    public override Option<RegisterToRegistry<LogRegistryName, ILogProperties>> registerToRegistry { get; }

    public UnityLog(Option<RegisterToRegistry<LogRegistryName, ILogProperties>> registerToRegistry) {
      this.registerToRegistry = registerToRegistry;
    }

    [LazyProperty] new static ILog log => Log.d.withScope(nameof(UnityLog));

    protected override void logInner(LogLevel l, LogEntry entry) {
      switch (l) {
        case LogLevel.VERBOSE:
        case LogLevel.DEBUG:
        case LogLevel.INFO:
          Debug.Log(s(entry), entry.maybeContext as Object);
          break;
        case LogLevel.WARN:
          Debug.LogWarning(s(entry), entry.maybeContext as Object);
          break;
        case LogLevel.ERROR:
          Debug.LogError(s(entry), entry.maybeContext as Object);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(l), l, null);
      }
    }

    static string s(LogEntry e) => $"{MESSAGE_PREFIX}{e}";

    static Try<LogEvent> convertUnityMessageToLogEvent(
      string message, string backtraceS, LogType type, int stackFramesToSkipWhenGenerating
    ) {
      try {
        using var _ = markerConvertUnityMessageToLogEvent.Auto();
        var level = convertLevel(type);

        // We want to collect backtrace on the current thread
        var backtrace =
          level >= LogLevel.WARN
            ?
              // backtrace may be empty in release mode.
              string.IsNullOrEmpty(backtraceS)
                ? Backtrace.generateFromHere(stackFramesToSkipWhenGenerating + 1 /*this stack frame*/)
                : Backtrace.parseStringBacktraceOptimized(backtraceS, BacktraceElemUnity.parseBacktraceLineNonAlloc)
            : None._;
        var logEvent = new LogEvent(level, new LogEntry(
          message, reportToErrorTracking: true, backtrace: backtrace.toNullable()
        ));
        return F.scs(logEvent);
      }
      catch (Exception e) {
        return F.err<LogEvent>(e);
      }
    }

    public static readonly LazyVal<IRxObservable<LogEvent>> fromUnityLogMessages = Lazy.a(() =>
      Observable.fromEvent2<LogEvent, Application.LogCallback>(
        onEvent => {
          Application.logMessageReceivedThreaded += callback;
          return callback;
          
          void callback(string message, string backtrace, LogType type) {
            // Ignore messages that we ourselves sent to Unity.
            if (message.StartsWithFast(MESSAGE_PREFIX)) return;
            var logEventTry = convertUnityMessageToLogEvent(
              message, backtrace, type, stackFramesToSkipWhenGenerating: 1 /* This stack frame */
            );
            var logEvent = logEventTry.isSuccess
              ? logEventTry.__unsafeGet
              : new LogEvent(
                LogLevel.ERROR, 
                LogEntry.fromException(
                  $"Error while converting Unity log message (type: {type}): {message}, backtrace: [{backtrace}]", 
                  logEventTry.__unsafeException
                ));

            if (OnMainThread.isMainThread) {
              tryToLogEvent(logEvent);
            }
            else {
              logOnMainThread(logEvent);
            }
          }

          // Separate method to avoid allocation if we are on main thread already.
          void logOnMainThread(LogEvent logEvent) =>
            OnMainThread.run(
              () => tryToLogEvent(logEvent),
              runNowIfOnMainThread: true
            );

          void tryToLogEvent(LogEvent logEvent) {
            try {
              onEvent(logEvent);
            }
            catch (Exception e) {
              // subscriber may throw an exception
              // log that exception to our logger to prevent endless loop
              log.error(e);
            }
          }
        },
        callback => Application.logMessageReceivedThreaded -= callback
      )
    );

    static LogLevel convertLevel(LogType type) {
      switch (type) {
        case LogType.Error:
        case LogType.Exception:
        case LogType.Assert:
          return LogLevel.ERROR;
        case LogType.Warning:
          return LogLevel.WARN;
        case LogType.Log:
          return LogLevel.INFO;
        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }
}