using System;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.unity.Components.DebugConsole;
using FPCSharpUnity.unity.Utilities;
using JetBrains.Annotations;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Dispose;
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;
using Debug = UnityEngine.Debug;

namespace FPCSharpUnity.unity.Logger {
  /// <summary>
  /// Default logger for the Unity applications. You want to use <see cref="d"/> method for your logging.
  /// </summary>
  [PublicAPI] public static class Log {
    // InitializeOnLoad is needed to set static variables on main thread.
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod]
    static void init() {}

    public static readonly LogLevel defaultLogLevel =
      Application.isEditor || Debug.isDebugBuild
      ? LogLevel.DEBUG : LogLevel.INFO;

    static readonly bool useConsoleLog = EditorUtils.inBatchMode;

    /// <summary>Registers loggers to <see cref="DConsole"/> using the default registry.</summary>
    public static void registerToDConsole(ITracker tracker, DConsole dc) =>
      registerToDConsole(tracker, dc, static () => registry.registered);
    
    /// <summary>Registers loggers to <see cref="DConsole"/> using a function to get the loggers.</summary>
    public static void registerToDConsole(
      ITracker tracker, DConsole dc, Func<ImmutableDictionary<LogRegistryName, ILogProperties>> getRegistered
    ) {
      var levels = EnumUtils.GetValues<LogLevel>();
      
      dc.registerOnShow(tracker, console => {
        {
          var r = console.registrarFor("Loggers");
          r.register("List all", () => 
            getRegistered().OrderBySafe(_ => s(_.Key)).Select(kv => $"{s(kv.Key)}: {kv.Value.level}")
              .mkStringEnumNewLines()
          );
        }
        
        // Render other loggers
        var registered = getRegistered();
        foreach (var (name, log) in registered) {
          var r = console.registrarFor($"Log: {s(name)}");
          r.registerEnum("Level", Ref.a(() => log.level, v => log.level = v), levels);
        }
      });
    }

    public static readonly LogRegistryName DEFAULT_LOGGER_NAME = new LogRegistryName("Default"); 

    static ILog _default;
    public static ILog @default {
      get {
        if (_default == null) {
          var register = registry.register;
          _default = useConsoleLog ? new ConsoleLog(Some.a(register)) : new UnityLog(Some.a(register));
          // First subscribe and only then register, because subscription listens to the registration.
          LogLevelControl.subscribeToApplyOverridenLevels(
            registry,
            // This registration is permanent.
            NeverDisposeDisposableTracker.instance, 
            Some.a(_default)
          );
          register(new(DEFAULT_LOGGER_NAME, _default));
          
          // Set the global log if it's not set yet.
          GlobalLog.maybeLog |= Some.a(_default);
        }
        
        return _default;
      }
      set => _default = value;
    }

    /// <summary>The default registry that the <see cref="@default"/> logger registers to.</summary>
    public static readonly LogRegistry registry = new LogRegistry(); 

    /// <summary>
    /// Shorthand for <see cref="Log.@default"/>. Allows the following syntax:
    /// <code><![CDATA[
    /// Log.d.mInfo("foo");
    /// ]]></code>
    /// </summary>
    public static ILog d => @default;
  }
}
