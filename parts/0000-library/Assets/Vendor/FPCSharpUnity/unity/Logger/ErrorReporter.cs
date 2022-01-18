using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.reactive;

using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  public static class ErrorReporter {
    public readonly struct AppInfo {
      public readonly string bundleIdentifier, productName;
      public readonly VersionNumber bundleVersion;

      public AppInfo(string bundleIdentifier, VersionNumber bundleVersion, string productName) {
        this.bundleIdentifier = bundleIdentifier;
        this.bundleVersion = bundleVersion;
        this.productName = productName;
      }
    }

    [PublicAPI] public delegate void OnError(LogEvent data);

    [PublicAPI] public static readonly LazyVal<IRxObservable<LogEvent>> defaultStream =
      UnityLog.fromUnityLogMessages.lazyMap(o => o.join(Log.d.messageLoggedObs()));

    /// <summary>
    /// Report warnings and errors from default logger and unity log messages.
    /// </summary>
    [PublicAPI] 
    public static ISubscription registerDefault(
      this OnError onError, ITracker tracker, LogLevel logFrom
    ) =>
      defaultStream.strict
      .filter(e => e.entry.reportToErrorTracking && e.level >= logFrom)
      .subscribe(tracker, e => onError(e));
  }
}
