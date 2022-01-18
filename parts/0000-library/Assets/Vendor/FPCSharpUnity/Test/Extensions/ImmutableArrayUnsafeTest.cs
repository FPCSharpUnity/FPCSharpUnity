﻿using System.Collections.Immutable;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Extensions {
  public class ImmutableArrayUnsafeTest {
    [Test]
    public void NoGarbageCreateAndGetShouldWork() {
      int[] arr = {1, 2, 3};
      var iArr = ImmutableArrayUnsafe.createByMove(arr);
      iArr.shouldEqualEnum(
        ImmutableArray.Create(arr),
        "it should be the same as created via a safe way"
      );
      var arr2 = iArr.internalArray();
      arr2.shouldRefEqual(arr, "it should be the same reference");
    }
  }
}