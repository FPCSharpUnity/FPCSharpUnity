using System.Runtime.CompilerServices;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Utilities;
using JetBrains.Annotations;

namespace FPCSharpUnity.unity.Extensions {
  public static class UIntExts {
    [PublicAPI] public static int toIntOrLog(
      this uint a,
      LogLevel level = LogLevel.ERROR,
      [CallerMemberName] string callerMemberName = "",
      [CallerFilePath] string callerFilePath = "",
      [CallerLineNumber] int callerLineNumber = 0
    ) {
      if (a > int.MaxValue) {
        if (Log.d.willLog(level)) Log.d.log(
          level,
          $"{nameof(UIntExts)}.{nameof(toIntOrLog)} called with {a} from " +
          $"{callerMemberName} @ {callerFilePath}:{callerLineNumber}"
        );
        return int.MaxValue;
      }

      return (int) a;
    }
  }
}