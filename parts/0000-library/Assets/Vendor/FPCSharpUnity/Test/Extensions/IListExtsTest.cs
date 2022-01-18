﻿using System.Collections.Generic;
using System.Collections.Immutable;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.exts;

namespace FPCSharpUnity.unity.Extensions {
  public class IListTestHeadOption {
    [Test]
    public void Test() {
      F.emptyList<int>().headOption().shouldBeNone();
      F.list(0).headOption().shouldBeSome(0);
      F.list(0, 1).headOption().shouldBeSome(0);
    }
  }

  public static class IListExtsTestExts {
    public static IList<A> downcast<A>(this IImmutableList<A> l) => (IList<A>) l;
  }

  public class IListExtsTestIsEmpty {
    [Test]
    public void WhenEmpty() =>
      ImmutableList<int>.Empty.downcast().isEmptyAllocating().shouldBeTrue();

    [Test]
    public void WhenNonEmpty() =>
      ImmutableList.Create(1).downcast().isEmptyAllocating().shouldBeFalse();
  }

  public class IListExtsTestNonEmpty {
    [Test]
    public void WhenEmpty() =>
      ImmutableList<int>.Empty.downcast().nonEmptyAllocating().shouldBeFalse();

    [Test]
    public void WhenNonEmpty() =>
      ImmutableList.Create(1).downcast().nonEmptyAllocating().shouldBeTrue();
  }
}