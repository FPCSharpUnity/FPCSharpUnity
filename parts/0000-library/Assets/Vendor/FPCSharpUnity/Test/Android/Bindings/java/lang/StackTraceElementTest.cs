#if UNITY_ANDROID
using NUnit.Framework;
using FPCSharpUnity.core.test_framework;

namespace FPCSharpUnity.unity.Android.Bindings.java.lang {
  public class StackTraceElementTest {
    [Test]
    public void TestMethodName() {
      "FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar+<>c__DisplayClass9_0`2[FPCSharpUnity.unity.Functional.Unit,FPCSharpUnity.unity.Functional.Unit].<register>b__0 ()"
        .methodAsAndroid().shouldEqual(
          "FPCSharpUnity.unity.Components.DebugConsole.DConsoleRegistrar$$$c__DisplayClass9_0$2$FPCSharpUnity.unity.Functional.Unit$FPCSharpUnity.unity.Functional.Unit$.$register$b__0$$$"
        );
    }
  }
}
#endif