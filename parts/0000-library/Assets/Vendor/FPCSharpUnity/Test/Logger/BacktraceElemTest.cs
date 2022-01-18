using System.Collections.Immutable;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  class BacktraceElemTestParseUnityBacktraceLine {
    BacktraceElem elem(string method) => new BacktraceElem(method, F.none<BacktraceElem.FileInfo>());

    [Test]
    public void Test1() {
      BacktraceElemUnity.parseBacktraceLine(
        "UnityEngine.Debug:LogError(Object)"
      ).shouldEqual(
        elem("UnityEngine.Debug:LogError(Object)")
      );
      BacktraceElemUnity.parseBacktraceLine(
        "FPCSharpUnity.unity.Logger.Log:error(Object) (at Assets/Vendor/TLPLib/Logger/Log.cs:14)"
      ).shouldEqual(
        new BacktraceElem(
          "FPCSharpUnity.unity.Logger.Log:error(Object)",
          Some.a(new BacktraceElem.FileInfo("Assets/Vendor/TLPLib/Logger/Log.cs", 14))
        )
      );
      BacktraceElemUnity.parseBacktraceLine(
        "Assets.Code.Main:<Awake>m__32() (at Assets/Code/Main.cs:60)"
      ).shouldEqual(
        new BacktraceElem(
          "Assets.Code.Main:<Awake>m__32()",
          Some.a(new BacktraceElem.FileInfo("Assets/Code/Main.cs", 60))
        )
      );
      BacktraceElemUnity.parseBacktraceLine(
        "FPCSharpUnity.unity.Concurrent.<NextFrameEnumerator>c__IteratorF:MoveNext() (at Assets/Vendor/TLPLib/Concurrent/ASync.cs:175)"
      ).shouldEqual(
        new BacktraceElem(
          "FPCSharpUnity.unity.Concurrent.<NextFrameEnumerator>c__IteratorF:MoveNext()",
          Some.a(new BacktraceElem.FileInfo("Assets/Vendor/TLPLib/Concurrent/ASync.cs", 175))
        )
      );
    }

    [Test]
    public void Test2() {
      var actual = Backtrace.parseStringBacktrace(
@"FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass4_0.<register>b__0 ()
FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass5_0`1[FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit _)
FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass8_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit obj)
FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass9_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 ()
FPCSharpUnity.unity.Components.DebugConsole.DConsole+<>c__DisplayClass18_0.<showGroup>b__0 ()
UnityEngine.Events.InvokableCall.Invoke (System.Object[] args)",
        BacktraceElemUnity.parseBacktraceLine
      ).get.elements.a;
      var expected = ImmutableList.Create(
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass4_0.<register>b__0 ()"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass5_0`1[FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit _)"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass8_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit obj)"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass9_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 ()"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsole+<>c__DisplayClass18_0.<showGroup>b__0 ()"),
        elem("UnityEngine.Events.InvokableCall.Invoke (System.Object[] args)")
      );
      actual.shouldEqual(expected);
    }
  }
}