using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.serialization;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Dispose;
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
    /// registration. If <see cref="maybeLog"/> is provided, a message is logged
    /// </summary>
    public static void subscribeToApplyOverridenLevels(ILogRegistry registry, Option<ILog> maybeLog) {
      registry.onRegister.subscribe(DisposableTrackerU.disposeOnExitPlayMode, args => {
        {if (
          prefValDict.get(args.name).valueOut(out var prefVal) 
          && prefVal.value.valueOut(out var levelOverride)
          && args.log.level != levelOverride
        ) {
          {if (maybeLog.valueOut(out var log)) {
            log.mInfo($"Overriding log level on '{args.name.asString()}' from {args.log.level} to {levelOverride}.");
          }}

          args.log.level = levelOverride;
        }}
      });
    }
  }
}