using System;
using System.Text.RegularExpressions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  public static class BacktraceElemUnity {
    /*
    Example backtrace:
UnityEngine.Debug:LogError(Object)
FPCSharpUnity.unity.Logger.Log:error(Object) (at Assets/Vendor/TLPLib/Logger/Log.cs:14)
Assets.Code.Main:<Awake>m__32() (at Assets/Code/Main.cs:60)
FPCSharpUnity.unity.Concurrent.<NextFrameEnumerator>c__IteratorF:MoveNext() (at Assets/Vendor/TLPLib/Concurrent/ASync.cs:175)
    */
    public static readonly Regex UNITY_BACKTRACE_LINE = new Regex(@"^(.+?)( \(at (.*?):(\d+)\))?$");

    public static BacktraceElem parseBacktraceLine(string line) {
      var match = UNITY_BACKTRACE_LINE.Match(line);

      var method = match.Groups[1].Value;
      var hasLineNo = match.Groups[2].Success;
      return new BacktraceElem(
        method,
        hasLineNo
          ? Some.a(new BacktraceElem.FileInfo(match.Groups[3].Value, int.Parse(match.Groups[4].Value)))
          : Option<BacktraceElem.FileInfo>.None
      );
    }
    
    /// <summary>
    /// Non-allocating version of <see cref="parseBacktraceLine"/>
    /// </summary>
    public static BacktraceElem parseBacktraceLineNonAlloc(ReadOnlyMemory<char> line) {
      var asSpan = line.Span;
      var openParenIndex = asSpan.LastIndexOf(" (at ");
      var colonIndex = asSpan.LastIndexOf(':');
      var closeParenIndex = asSpan.LastIndexOf(')');
      var hasLineNo = openParenIndex >= 0 && colonIndex > openParenIndex && closeParenIndex >= colonIndex;

      var method = hasLineNo ? line[..openParenIndex] : line;

      return new BacktraceElem(
        method,
        hasLineNo
          ? Some.a(new BacktraceElem.FileInfo(
            line[(openParenIndex + 5)..colonIndex],
            int.Parse(asSpan[(colonIndex + 1)..closeParenIndex])
          ))
          : Option<BacktraceElem.FileInfo>.None
      );
    }

  }

  public static class BacktraceElemExts {
    /// <summary>Is this trace frame is in our application code?</summary>
    public static bool inApp(this BacktraceElem elem) => !elem.method.Span.StartsWithFast(nameof(UnityEngine) + ".");
  }
}
