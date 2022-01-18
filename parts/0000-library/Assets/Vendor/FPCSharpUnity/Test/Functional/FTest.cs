﻿using System.Collections.Immutable;
using FPCSharpUnity.core.test_framework;

namespace FPCSharpUnity.unity.Functional {
  public class FTestIArrayFill {
    public void WhenZeroElements() {
      F.iArrayFill(0, _ => 1).shouldEqual(ImmutableArray<int>.Empty);
    }

    public void WhenSomeElements() {
      F.iArrayFill(3, i => i * 2).shouldEqual(ImmutableArray.Create(0, 2, 4));
    }
  }
}
