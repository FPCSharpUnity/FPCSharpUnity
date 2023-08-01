using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.serialization;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Dispose;
using FPCSharpUnity.unity.Threads;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Logger {
  public static partial class LogLevelControl {
    [LazyProperty] public static PrefValDictionary<LogRegistryName, Option<LogLevel>> prefValDict =>
      PrefVal.player.dictionary<LogRegistryName, Option<LogLevel>>(
        "LogLevelControlWindow",
        keyToString: _ => _.name,
        SerializedRW.opt(SerializedRW.byte_.mapNoFail(b => (LogLevel) b, v => (byte) v)),
        defaultValue: None._,
        log: Log.d
      );

    /// <summary>
    /// Subscribes to <see cref="ILogRegistry.onRegister"/> events and overrides the levels of the loggers upon
    /// registration. If <see cref="maybeLog"/> is provided, a message is logged.
    /// <para/>
    /// The given <see cref="ITracker"/> is used to know when we should unsubscribe.
    /// </summary>
    public static void subscribeToApplyOverridenLevels(ILogRegistry registry, ITracker tracker, Option<ILog> maybeLog) {
      registry.onRegister.subscribe(tracker, args => {
        // Accessing prefvals requires main thread.
        OnMainThread.run(() => {
          {if (
            prefValDict.get(args.key).valueOut(out var prefVal) 
            && prefVal.value.valueOut(out var levelOverride)
            && args.value.level != levelOverride
          ) {
            maybeLog.ifSomeM(log => log.mInfo(
              $"Overriding log level on '{args.key.asString()}' from {args.value.level} to {levelOverride}."
            ));

            args.value.level = levelOverride;
          }}
        });
      });
    }
  }
}