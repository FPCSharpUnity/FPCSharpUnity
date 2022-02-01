﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.test_framework.spec;

namespace FPCSharpUnity.unity.Extensions {
  public class IEnumerableSpec : ImplicitSpecification {
    [Test] public void partitionCollect() => describe(() => {
      Option<string> collector(int i) => (i % 2 == 0).opt(i.ToString);

      when["empty"] = () => {
        var empty = ImmutableList<int>.Empty;
        var result = empty.partitionCollect(collector);

        it["should have nones empty"] = () => result.nones.shouldBeEmpty();
        it["should have somes empty"] = () => result.somes.shouldBeEmpty();
      };

      when["with elements"] = () => {
        var source = ImmutableList.Create(1, 2, 3, 4, 5, 6);
        var result = source.partitionCollect(collector);

        it["should collect nones"] = () => result.nones.shouldEqualEnum(1, 3, 5);
        it["should collect somes"] = () => result.somes.shouldEqualEnum("2", "4", "6");
      };
    });

    class TestObj {}

    [Test] public void mapDistinct() => describe(() => {
      it["should map multiple entries to same element"] = () => {
        var obj = new TestObj();
        new[] {1, 1, 1}.mapDistinct(a => obj).shouldEqualEnum(new [] {obj, obj, obj});
      };

      it["should only invoke mapper once for a unique element"] = () => {
        var invocations = new Dictionary<int, int>();
        new[] {1, 2, 3, 4, 1, 2, 3, 4}.mapDistinct(a => {
          invocations[a] = invocations.getOrElse(a, 0) + 1;
          return a;
        }).ToList().forSideEffects();
        invocations.shouldNotContain(kv => kv.Value != 1);
      };
    });

    [Test]
    public void collect() => describe(() => {
      when["with indexes"] = () => {
        it["should emit correct indexes"] = () => {
          new[] {"foo", "bar", "baz"}.collect((a, idx) => Some.a(idx)).shouldEqualEnum(0, 1, 2);
        };

        it["should only keep somes"] = () => {
          new[] {"foo", "bar", "baz"}.collect((a, idx) => (idx % 2 == 0).opt(a)).shouldEqualEnum("foo", "baz");
        };
      };
    });

    [Test]
    public void slidingWindow() => describe(() => {
      when["empty"] = () => {
        it["should return empty enumerable"] = () => Enumerable.Empty<int>().slidingWindow(2).shouldBeEmpty();
      };

      when["window size is 0"] = () => {
        it["should throw an exception"] = () =>
          Assert.Throws(
            typeof(ArgumentException),
            // We need to evaluate lazy enumerable for exception to be thrown
            () => new[] {1, 2, 3}.slidingWindow(0).ToArray().forSideEffects()
          );
      };
      
      when["less elements than window"] = () => {
        it["should return empty enumerable"] = () => new[] {1, 2}.slidingWindow(3).shouldBeEmpty();
      };

      when["elements == window size"] = () => {
        it["should return single window"] = () =>
          new[] {1, 2, 3}.slidingWindow(3).shouldEqualEnum(new[] {1, 2, 3});
      };

      when["more elements than in window"] = () => {
        var src = new[] {1, 2, 3, 4, 5, 6, 7};

        when["window size == 1"] = () => {
          it["should return multiple windows"] = () =>
            src.slidingWindow(1).shouldEqualEnum(
              new[] {1}, new[] {2}, new[] {3}, new[] {4}, new[] {5}, new[] {6}, new[] {7}
            );
        };

        when["window size == 2"] = () => {
          it["should return multiple windows"] = () =>
            src.slidingWindow(2).shouldEqualEnum(
              new[] {1, 2}, new[] {2, 3}, new[] {3, 4}, new[] {4, 5}, new[] {5, 6}, new[] {6, 7}
            );
        };

        when["window size == 3"] = () => {
          it["should return multiple windows"] = () =>
            src.slidingWindow(3).shouldEqualEnum(
              new[] {1, 2, 3}, new[] {2, 3, 4}, new[] {3, 4, 5}, new[] {4, 5, 6}, new[] {5, 6, 7}
            );
        };

        when["window size == 4"] = () => {
          it["should return multiple windows"] = () =>
            src.slidingWindow(4).shouldEqualEnum(
              new[] {1, 2, 3, 4}, new[] {2, 3, 4, 5}, new[] {3, 4, 5, 6}, new[] {4, 5, 6, 7}
            );
        };
      };
    });
  }

  public class IEnumerableTestPartition {
    [Test]
    public void TestEquals() {
      var s = F.list(1, 2);
      var p1 = s.partition(_ => true);
      var p2 = s.partition(_ => false);
      var p3 = s.partition(_ => true);
      p1.Equals(p2).shouldBeFalse();
      p1.Equals(p3).shouldBeTrue();
    }

    [Test]
    public void Test() {
      var source = ImmutableList.Create(1, 2, 3, 4, 5);
      var empty = ImmutableList<int>.Empty;

      var emptyPartition = new int[] {}.partition(_ => true);
      emptyPartition.trues.shouldEqual(empty);
      emptyPartition.falses.shouldEqual(empty);

      var alwaysFalse = source.partition(_ => false);
      alwaysFalse.trues.shouldEqual(empty);
      alwaysFalse.falses.shouldEqual(source);

      var alwaysTrue = source.partition(_ => true);
      alwaysTrue.trues.shouldEqual(source);
      alwaysTrue.falses.shouldEqual(empty);

      var normal = source.partition(_ => _ <= 3);
      normal.trues.shouldEqual(ImmutableList.Create(1, 2, 3));
      normal.falses.shouldEqual(ImmutableList.Create(4, 5));
    }
  }

  public class IEnumerableTestZip {
    [Test]
    public void TestWhenEmpty() =>
      ImmutableList<int>.Empty.zip(ImmutableList<string>.Empty)
      .shouldEqual(ImmutableList<(int, string)>.Empty);

    [Test]
    public void TestWhenEqual() =>
      ImmutableList.Create(1, 2, 3).zip(ImmutableList.Create("a", "b", "c"), (a, b) => b + a)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3"));

    [Test]
    public void TestWhenLeftShorter() =>
      ImmutableList.Create(1, 2, 3).zip(ImmutableList.Create("a", "b", "c", "d", "e"), (a, b) => b + a)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3"));

    [Test]
    public void TestWhenRightShorter() =>
      ImmutableList.Create(1, 2, 3, 4, 5).zip(ImmutableList.Create("a", "b", "c"), (a, b) => b + a)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3"));
  }

  public class IEnumerableTestZipLeft {
    [Test]
    public void TestWhenEmpty() =>
      ImmutableList<int>.Empty
      .zipLeft(ImmutableList<string>.Empty, Tpl.a, (a, idx) => Tpl.a(a, idx.ToString()))
      .shouldEqual(ImmutableList<Tpl<int, string>>.Empty);

    [Test]
    public void TestWhenLeftEmpty() =>
      ImmutableList<int>.Empty
      .zipLeft(ImmutableList.Create("a", "b", "c"), Tpl.a, (a, idx) => Tpl.a(a, idx.ToString()))
      .shouldEqual(ImmutableList<Tpl<int, string>>.Empty);

    [Test]
    public void TestWhenRightEmpty() =>
      ImmutableList.Create(1, 2, 3)
      .zipLeft(ImmutableList<string>.Empty, (a, b) => a + b, (a, idx) => idx.ToString() + a)
      .shouldEqual(ImmutableList.Create("01", "12", "23"));

    [Test]
    public void TestWhenEqualLength() =>
      ImmutableList.Create(1, 2, 3)
      .zipLeft(ImmutableList.Create("a", "b", "c"), (a, b) => a + b, (a, idx) => idx.ToString() + a)
      .shouldEqual(ImmutableList.Create("1a", "2b", "3c"));

    [Test]
    public void TestWhenLeftShorter() =>
      ImmutableList.Create(1, 2, 3)
      .zipLeft(ImmutableList.Create("a", "b", "c", "d", "e"), (a, b) => b + a, (a, idx) => idx.ToString() + a)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3"));

    [Test]
    public void TestWhenRightShorter() =>
      ImmutableList.Create(1, 2, 3, 4, 5)
      .zipLeft(ImmutableList.Create("a", "b", "c"), (a, b) => b + a, (a, idx) => idx.ToString() + a)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3", "34", "45"));
  }

  public class IEnumerableTestZipRight {
    [Test]
    public void TestWhenEmpty() =>
      ImmutableList<int>.Empty
      .zipRight(ImmutableList<string>.Empty, Tpl.a, (b, idx) => Tpl.a(idx, b))
      .shouldEqual(ImmutableList<Tpl<int, string>>.Empty);

    [Test]
    public void TestWhenLeftEmpty() =>
      ImmutableList<int>.Empty
      .zipRight(ImmutableList.Create("a", "b", "c"), Tpl.a, (b, idx) => Tpl.a(idx, b))
      .shouldEqual(ImmutableList.Create(Tpl.a(0, "a"), Tpl.a(1, "b"), Tpl.a(2, "c")));

    [Test]
    public void TestWhenRightEmpty() =>
      ImmutableList.Create(1, 2, 3)
      .zipRight(ImmutableList<string>.Empty, Tpl.a, (b, idx) => Tpl.a(idx, b))
      .shouldEqual(ImmutableList<Tpl<int,string>>.Empty);

    [Test]
    public void TestWhenEqualLength() =>
      ImmutableList.Create(1, 2, 3)
      .zipRight(ImmutableList.Create("a", "b", "c"), (a, b) => b + a, (b, idx) => b + idx)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3"));

    [Test]
    public void TestWhenLeftShorter() =>
      ImmutableList.Create(1, 2, 3)
      .zipRight(ImmutableList.Create("a", "b", "c", "d", "e"), (a, b) => b + a, (b, idx) => b + idx)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3", "d3", "e4"));

    [Test]
    public void TestWhenRightShorter() =>
      ImmutableList.Create(1, 2, 3, 4, 5)
      .zipRight(ImmutableList.Create("a", "b", "c"), (a, b) => b + a, (b, idx) => b + idx)
      .shouldEqual(ImmutableList.Create("a1", "b2", "c3"));
  }
}
