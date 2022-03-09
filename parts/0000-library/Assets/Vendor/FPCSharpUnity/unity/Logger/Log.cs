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
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;
using Debug = UnityEngine.Debug;

namespace FPCSharpUnity.unity.Logger {
  /// <summary>
  /// Default logger for the Unity applications. You want to use <see cref="d"/> method for your logging.
  /// </summary>
  [PublicAPI] public static class Log {
    // InitializeOnLoad is needed to set static variables on main thread.
    // FKRs work without it, but on Gummy Bear repo tests fail
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod]
    static void init() {}

    public static readonly LogLevel defaultLogLevel =
      Application.isEditor || Debug.isDebugBuild
      ? LogLevel.DEBUG : LogLevel.INFO;

    static readonly bool useConsoleLog = EditorUtils.inBatchMode;

    /// <summary>Registers loggers to DConsole using the default registry.</summary>
    public static void registerToDConsole(ITracker tracker) =>
      registerToDConsole(tracker, () => registry.registered);
    
    public static void registerToDConsole(
      ITracker tracker, Func<ImmutableDictionary<LogRegistryName, ILogProperties>> getRegistered
    ) {
      var levels = EnumUtils.GetValues<LogLevel>();
      
      DConsole.instance.registerOnShow(tracker, console => {
        var registered = getRegistered();

        {
          var r = console.registrarFor("Loggers", tracker, persistent: false);
          r.register("List all", () => 
            registered.OrderBySafe(_ => s(_.Key)).Select(kv => $"{s(kv.Key)}: {kv.Value.level}").mkStringEnumNewLines()
          );
        }

        // Render other loggers
        foreach (var (name, log) in registered) {
          var r = console.registrarFor($"Log: {s(name)}", tracker, persistent: false);
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
          LogLevelControl.subscribeToApplyOverridenLevels(registry, Some.a(_default));
          register(new(_default, DEFAULT_LOGGER_NAME));
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
