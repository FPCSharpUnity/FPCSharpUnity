﻿using FPCSharpUnity.core.test_framework;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Logger {
  public class LogTestDefaultLogLevel {
    [Test]
    public void DefaultLogLevelForDefaultLoggerShouldBeSet() {
      // This can fail if we have a circular dependency amongst static fields. C# silently
      // ignores that value can't be resolved and assigns it to a default value. Yay!
      Log.@default.level.shouldEqual(Log.defaultLogLevel);
    }
  }
}