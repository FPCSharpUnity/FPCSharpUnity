using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.config;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Configuration {
  public static class ConfigLookupErrorExts {
    public static LogEntry toLogEntry(
      this ConfigLookupError err, string message, ICollection<KeyValuePair<string, string>> extraExtras = null
    ) {
      if (err.kind == ConfigLookupError.Kind.EXCEPTION) {
        return LogEntry.fromException(nameof(ConfigLookupError), err.exception.__unsafeGet);
      }
      var extras = extraExtras == null ? err.extras : err.extras.Concat(extraExtras).toImmutableArrayC();

      return new LogEntry($"{message}: {err.kind}", extras: extras);
    }
  }
}