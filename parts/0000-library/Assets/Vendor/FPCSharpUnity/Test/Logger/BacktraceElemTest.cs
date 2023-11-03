using System;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;

namespace FPCSharpUnity.unity.Logger {
  class BacktraceElemTestParseUnityBacktraceLine {
    BacktraceElem elem(string method) => new BacktraceElem(method, F.none<BacktraceElem.FileInfo>());

    const string sampleStackTrace1 =
      @"FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass4_0.<register>b__0 ()
FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass5_0`1[FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit _)
FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass8_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit obj)
FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass9_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 ()
FPCSharpUnity.unity.Components.DebugConsole.DConsole+<>c__DisplayClass18_0.<showGroup>b__0 ()
UnityEngine.Events.InvokableCall.Invoke (System.Object[] args)";
    
    const string sampleStackTrace2 =
      @"UnityEngine.Debug:Log (object,UnityEngine.Object)
FPCSharpUnity.unity.Logger.UnityLog:logInner (FPCSharpUnity.core.log.LogLevel,FPCSharpUnity.core.log.LogEntry) (at generated-by-compiler/FPCSharpUnity.unity/macros/Assets/Vendor/FPCSharpUnity/unity/Logger/UnityLog.transformed.cs:43)
FPCSharpUnity.unity.Logger.LogBase:logInternal (FPCSharpUnity.core.log.LogLevel,FPCSharpUnity.core.log.LogEntry) (at generated-by-compiler/FPCSharpUnity.unity/macros/Assets/Vendor/FPCSharpUnity/unity/Logger/LogBase.transformed.cs:16)
FPCSharpUnity.core.log.BaseLog:logRaw (FPCSharpUnity.core.log.LogLevel,FPCSharpUnity.core.log.LogEntry) (at D:/work/sb2/quantum_code/FPCSharpUnity.core/log/BaseLog.cs:12)
FPCSharpUnity.core.log.MappingDelegatingLog:logRaw (FPCSharpUnity.core.log.LogLevel,FPCSharpUnity.core.log.LogEntry) (at D:/work/sb2/quantum_code/FPCSharpUnity.core/log/MappingLog.cs:26)
FPCSharpUnity.core.log.ILogExts:info (FPCSharpUnity.core.log.ILog,FPCSharpUnity.core.log.LogEntry,FPCSharpUnity.core.data.CallerData) (at D:/work/sb2/quantum_code/FPCSharpUnity.core/generated-by-compiler/FPCSharpUnity.core/macros/log/ILog.transformed.cs:306)
FPCSharpUnity.core.log.ILogExts:info (FPCSharpUnity.core.log.ILog,string,object,FPCSharpUnity.core.data.CallerData) (at D:/work/sb2/quantum_code/FPCSharpUnity.core/generated-by-compiler/FPCSharpUnity.core/macros/log/ILog.transformed.cs:288)
Game.code.data.SingletonScriptableObjectLocator`1<Game.code.data.AppVersionSO>:get_instance () (at generated-by-compiler/game/macros/Assets/Game/code/data/SingletonScriptableObjectLocator.transformed.cs:47)
Game.code.data.AppVersionSO:get_instance () (at generated-by-compiler/game/macros/Assets/Game/code/data/AppVersionSO.transformed.cs:35)
Game.code.control.entry_point.WindowTitleManager:.ctor () (at generated-by-compiler/game/macros/Assets/Game/code/control/entry_point/WindowTitleManager.transformed.cs:40)
Game.code.control.entry_point.WindowTitleManager:reset () (at generated-by-compiler/game/macros/Assets/Game/code/control/entry_point/WindowTitleManager.transformed.cs:26)
";

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
        sampleStackTrace1,
        BacktraceElemUnity.parseBacktraceLine
      ).get.elements.neVal;
      var expected = ImmutableArrayC.create(
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass4_0.<register>b__0 ()"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass5_0`1[FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit _)"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass8_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 (Unit obj)"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass9_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 ()"),
        elem("FPCSharpUnity.unity.Components.DebugConsole.DConsole+<>c__DisplayClass18_0.<showGroup>b__0 ()"),
        elem("UnityEngine.Events.InvokableCall.Invoke (System.Object[] args)")
      );
      actual.shouldEqual(expected);
    }
    
    [Test]
    public void TestOptimization1() {
      var actual = Backtrace.parseStringBacktraceOptimized(
        sampleStackTrace1, BacktraceElemUnity.parseBacktraceLineNonAlloc
      ).get.elements.neVal;
      var expected = Backtrace.parseStringBacktrace(
        sampleStackTrace1, BacktraceElemUnity.parseBacktraceLine
      ).get.elements.neVal;
      actual.shouldEqual(expected);
    }
    
    [Test]
    public void TestOptimization2() {
      var actual = Backtrace.parseStringBacktraceOptimized(
        sampleStackTrace2, BacktraceElemUnity.parseBacktraceLineNonAlloc
      ).get.elements.neVal;
      var expected = Backtrace.parseStringBacktrace(
        sampleStackTrace2, BacktraceElemUnity.parseBacktraceLine
      ).get.elements.neVal;
      actual.shouldEqual(expected);
    }
  }
}