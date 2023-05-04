﻿using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.reactive;

using NUnit.Framework;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.test_framework;

namespace FPCSharpUnity.unity.Concurrent {
  static class FT {
    public static readonly Func<int, Either<int, string>> left = F.left<int, string>;
    public static readonly Func<string, Either<int, string>> right = F.right<int, string>;

    public static IEnumerable<Future<A>> addUnfulfilled<A>(this IEnumerable<Future<A>> futures)
      { return futures.Concat(Future<A>.unfulfilled.yield()); }
  }

  public class FutureTestEquality : TestBase {
    [Test]
    public void Equals() {
      var asyncF = Future.async<int>(out var asyncP);
      var unfullfilled = Future.unfulfilled;
      var completed = Future.successful(3);

      shouldNotEqualSymmetrical(unfullfilled, completed);

      shouldBeIdentical(unfullfilled, asyncF);
      shouldNotEqualSymmetrical(asyncF, completed);
      asyncP.complete(3);
      shouldNotEqualSymmetrical(unfullfilled, asyncF);
      shouldBeIdentical(asyncF, completed);
    }
  }

  public class FutureTestOnComplete {
    const int value = 1;

    [Test]
    public void WhenSuccessful() {
      var f = Future.successful(value);
      var result = 0;
      f.onComplete(i => result = i);
      result.shouldEqual(value, "it should run the function immediately");
    }

    [Test]
    public void WhenUnfulfilled() {
      var f = Future<int>.unfulfilled;
      var result = 0;
      f.onComplete(i => result = i);
      result.shouldEqual(0, "it should not run the function");
    }

    [Test]
    public void WhenASync() {
      var f = Future.async<int>(out var p);

      var result = 0;
      f.onComplete(i => result = i);
      result.shouldEqual(0, "it should not run the function immediately");
      p.complete(value);
      result.shouldEqual(value, "it run the function after completion");
    }
  }

  public class FutureTestNowAndOnComplete {
    [Test]
    public void WhenSuccessful() {
      var f = Future.successful(1);
      var result = 0;
      f.nowAndOnComplete(iOpt => result += iOpt.fold(-1, _ => _));
      result.shouldEqual(1, "it should run the function once");
    }

    [Test]
    public void WhenUnfulfilled() {
      var f = Future<int>.unfulfilled;
      var result = 0;
      f.nowAndOnComplete(iOpt => result += iOpt.fold(-1, _ => _));
      result.shouldEqual(-1, "it should run the function once");
    }

    [Test]
    public void WhenASync() {
      var f = Future.async<int>(out var p);

      var result = 0;
      f.nowAndOnComplete(iOpt => result += iOpt.fold(-1, _ => _));
      result.shouldEqual(-1, "it should run the function immediately");
      p.complete(2);
      result.shouldEqual(1, "it run the function after completion again");
    }
  }

  public class FutureTestMap {
    readonly Func<int, int> mapper = i => i * 2;

    [Test]
    public void WhenSuccessful() {
      Future.successful(1).map(mapper).shouldBeOfSuccessfulType(2);
    }

    [Test]
    public void WhenUnfulfilled() {
      Future<int>.unfulfilled.map(mapper).shouldBeOfUnfulfilledType();
    }

    [Test]
    public void WhenASync() {
      var f = Future.async(out Promise<int> p);
      var f2 = f.map(mapper);
      f2.type.shouldEqual(FutureType.ASync);
      f2.value.shouldBeNone("it should not have value before original future completion");
      p.complete(1);
      f2.value.shouldBeSome(2, "it should have value after original future completion");
    }
  }

  public class FutureTestFlatMap {
    readonly Func<int, Future<int>> successfulMapper = i => Future.successful(i * 2);
    readonly Func<int, Future<int>> unfulfilledMapper = i => Future<int>.unfulfilled;

    readonly Future<int>
      successful = Future.successful(1),
      unfulfilled = Future<int>.unfulfilled;

    [Test]
    public void SuccessfulToSuccessful() {
      successful.flatMap(successfulMapper).shouldBeOfSuccessfulType(2);
    }
    [Test]
    public void SuccessfulToUnfulfilled() {
      successful.flatMap(unfulfilledMapper).shouldBeOfUnfulfilledType();
    }
    [Test]
    public void SuccessfulToASync() {
      var f2 = Future.async<int>(out var p2);
      var f = successful.flatMap(_ => f2);
      f.type.shouldEqual(FutureType.ASync);
      f.value.shouldBeNone("it should be uncompleted if source future is incomplete");
      p2.complete(2);
      f.value.shouldBeSome(2, "it should complete after completing the source future");
    }

    [Test]
    public void UnfulfilledToSuccessful() {
      unfulfilledShouldNotCallMapper(successfulMapper);
    }
    [Test]
    public void UnfulfilledToUnfulfilled() {
      unfulfilledShouldNotCallMapper(unfulfilledMapper);
    }
    [Test]
    public void UnfulfilledToASync() {
      unfulfilledShouldNotCallMapper(i => Future.a<int>(p => {}));
    }

    void unfulfilledShouldNotCallMapper<A>(Func<int, Future<A>> mapper) {
      var called = false;
      unfulfilled.flatMap(i => {
        called = true;
        return mapper(i);
      }).shouldBeOfUnfulfilledType();
      called.shouldBeFalse("it should not call the mapper");
    }

    [Test]
    public void ASyncToSuccessful() {
      var f = Future.async<int>(out var p);
      var called = false;
      var f2 = f.flatMap(i => {
        called = true;
        return Future.successful(i);
      });
      f2.type.shouldEqual(FutureType.ASync);
      f2.value.shouldBeNone();
      called.shouldBeFalse("it should not call function until completion of a source promise");
      p.complete(1);
      called.shouldBeTrue();
      f2.value.shouldBeSome(1);
    }

    [Test]
    public void ASyncToUnfulfilled() {
      var f = Future.async<int>(out var p);
      var called = false;
      var f2 = f.flatMap(_ => {
        called = true;
        return Future<int>.unfulfilled;
      });
      f2.type.shouldEqual(FutureType.ASync);
      f2.value.shouldBeNone();
      called.shouldBeFalse();
      p.complete(1);
      called.shouldBeTrue();
      f2.value.shouldBeNone("it shouldn't complete even if source future is completed");
    }

    [Test]
    public void ASyncToASync() {
      Promise<int> p1;
      var f1 = Future.async<int>(out p1);
      Promise<int> p2;
      var f2 = Future.async<int>(out p2);

      var called = false;
      var f = f1.flatMap(_ => {
        called = true;
        return f2;
      });
      f.type.shouldEqual(FutureType.ASync);
      f.value.shouldBeNone("it should be not completed at start");
      called.shouldBeFalse();
      p1.complete(1);
      called.shouldBeTrue();
      f.value.shouldBeNone("it should be not completed if source future completes");
      p2.complete(2);
      f.value.shouldBeSome(2, "it should be completed");
    }
  }

  public class FutureTestZip {
    [Test]
    public void WhenEitherSideUnfulfilled() {
      foreach (var t in new[] {
        Tpl.a("X-O", Future<int>.unfulfilled, Future.successful(1)),
        Tpl.a("O-X", Future.successful(1), Future<int>.unfulfilled)
      }) {
        var (name, fa, fb) = t;
        fa.zip(fb).shouldBeOfUnfulfilledType(name);
      }
    }

    [Test]
    public void WhenBothSidesSuccessful() {
      Future.successful(1).zip(Future.successful(2)).shouldBeOfSuccessfulType((1, 2));
    }

    [Test]
    public void WhenASync() {
      whenASync(true);
      whenASync(false);
    }

    static void whenASync(bool completeFirst) {
      Promise<int> p1, p2;
      var f1 = Future.async<int>(out p1);
      var f2 = Future.async<int>(out p2);
      var f = f1.zip(f2);
      f.type.shouldEqual(FutureType.ASync);
      f.value.shouldBeNone();
      (completeFirst ? p1 : p2).complete(completeFirst ? 1 : 2);
      f.value.shouldBeNone("it should not complete just from one side");
      (completeFirst ? p2 : p1).complete(completeFirst ? 2 : 1);
      f.value.shouldBeSome((1, 2), "it should complete from both sides");
    }
  }

  public class FutureTestFirstOf {
    [Test]
    public void WhenHasCompleted() {
      new[] {
        Future.unfulfilled,
        Future.unfulfilled,
        Future.successful(1),
        Future.unfulfilled,
        Future.unfulfilled
      }.firstOf().value.__unsafeGet.shouldEqual(1);
    }

    [Test]
    public void WhenHasMultipleCompleted() {
      new[] {
        Future.unfulfilled,
        Future.unfulfilled,
        Future.successful(1),
        Future.unfulfilled,
        Future.successful(2),
        Future.unfulfilled
      }.firstOf().value.__unsafeGet.shouldEqual(1);
    }

    [Test]
    public void WhenNoCompleted() {
      new[] {
        Future<int>.unfulfilled,
        Future<int>.unfulfilled,
        Future<int>.unfulfilled,
        Future<int>.unfulfilled
      }.firstOf().value.shouldBeNone();
    }
  }

  public class FutureTestFirstOfWhere {
    [Test]
    public void ItemFound() {
      new[] {1, 3, 5, 6, 7}.
        Select(Future.successful).firstOfWhere(i => (i % 2 == 0).opt(i)).
        value.__unsafeGet.shouldEqual(Some.a(6));
    }
    [Test]
    public void MultipleItemsFound() {
      new[] {1, 3, 5, 6, 7, 8}.
        Select(Future.successful).firstOfWhere(i => (i % 2 == 0).opt(i)).
        value.__unsafeGet.shouldEqual(Some.a(6));
    }

    [Test]
    public void ItemNotFound() {
      new[] {1, 3, 5, 7}.
        Select(Future.successful).firstOfWhere(i => (i % 2 == 0).opt(i)).
        value.__unsafeGet.shouldBeNone();
    }

    [Test]
    public void ItemNotFoundNotCompleted() {
      new[] {1, 3, 5, 7}.Select(Future.successful).addUnfulfilled().
        firstOfWhere(i => (i % 2 == 0).opt(i)).
        value.shouldBeNone();
    }
  }

  public class FutureTestFirstOfSuccessful {
    [Test]
    public void RightFound() {
      new[] { FT.left(1), FT.left(3), FT.left(5), FT.right("6"), FT.left(7) }.
        Select(Future.successful).firstOfSuccessful().
        value.__unsafeGet.shouldBeSome("6");
    }

    [Test]
    public void MultipleRightsFound() {
      new[] { FT.left(1), FT.left(3), FT.left(5), FT.right("6"), FT.left(7), FT.right("8") }.
        Select(Future.successful).firstOfSuccessful().
        value.__unsafeGet.shouldBeSome("6");
    }

    [Test]
    public void RightNotFound() {
      new[] { FT.left(1), FT.left(3), FT.left(5), FT.left(7) }.
        Select(Future.successful).firstOfSuccessful().
        value.__unsafeGet.shouldBeNone();
    }

    [Test]
    public void RightNotFoundNoComplete() {
      new[] { FT.left(1), FT.left(3), FT.left(5), FT.left(7) }.
        Select(Future.successful).addUnfulfilled().firstOfSuccessful().
        value.shouldBeNone();
    }
  }

  public class FutureTestFirstOfSuccessfulCollect {
    [Test]
    public void ItemFound() {
      new [] { FT.left(1), FT.left(2), FT.right("a"), FT.left(3) }.
        Select(Future.successful).firstOfSuccessfulCollect().value.__unsafeGet.
        shouldEqual(F.right<int[], string>("a"));
    }

    [Test]
    public void MultipleItemsFound() {
      new [] { FT.left(1), FT.left(2), FT.right("a"), FT.left(3), FT.right("b") }.
        Select(Future.successful).firstOfSuccessfulCollect().value.__unsafeGet.
        shouldEqual(F.right<int[], string>("a"));
    }

    [Test]
    public void ItemNotFound() {
      new [] { FT.left(1), FT.left(2), FT.left(3), FT.left(4) }.
        Select(Future.successful).firstOfSuccessfulCollect().value.__unsafeGet.
        leftValue.get.asDebugString().shouldEqual(new[] { 1, 2, 3, 4 }.asDebugString());
    }

    [Test]
    public void ItemNotFoundNoCompletion() {
      new [] { FT.left(1), FT.left(2), FT.left(3), FT.left(4) }.
        Select(Future.successful).addUnfulfilled().firstOfSuccessfulCollect().value.shouldBeNone();
    }
  }

  public class FutureTestFilter {
    [Test]
    public void CompleteToNotComplete() {
      Future.successful(3).filter(i => false).shouldNotBeCompleted();
    }

    [Test]
    public void CompleteToComplete() {
      Future.successful(3).filter(i => true).shouldBeCompleted(3);
    }

    [Test]
    public void NotCompleteToNotComplete() {
      Future<int>.unfulfilled.filter(i => false).shouldNotBeCompleted();
      Future<int>.unfulfilled.filter(i => true).shouldNotBeCompleted();
    }
  }

  public class FutureTestCollect {
    [Test]
    public void CompleteToNotComplete() {
      Future.successful(3).collect(i => F.none<int>()).shouldNotBeCompleted();
    }

    [Test]
    public void CompleteToComplete() {
      Future.successful(3).collect(i => Some.a(i * 2)).shouldBeCompleted(6);
    }

    [Test]
    public void NotCompleteToNotComplete() {
      Future<int>.unfulfilled.collect(i => F.none<int>()).shouldNotBeCompleted();
      Future<int>.unfulfilled.collect(Some.a).shouldNotBeCompleted();
    }
  }

  public class FutureTestDelay {
    [Test]
    public void Test() {
      var d = Duration.fromSeconds(1);
      var tc = new TestTimeContext();
      var f = Future.delay(d, 3, tc);
      f.value.shouldBeNone();
      tc.timePassed = d / 2;
      f.value.shouldBeNone();
      tc.timePassed = d;
      f.value.shouldBeSome(3);
    }
  }

  public class FutureTestDelayFrames {
    [Test] public void Test() => Assert.Ignore("TODO: test with integration tests");
  }

  public class FutureTestDelayUntilSignal {
    [Test]
    public void NotCompletedThenSignal() {
      var t = Future<int>.unfulfilled.delayUntilSignal();
      t.future.shouldNotBeCompleted();
      t.sendSignal();
      t.future.shouldNotBeCompleted();
    }

    [Test]
    public void NotCompletedThenCompletionThenSignal() {
      var t = Future.async(out Promise<Unit> p).delayUntilSignal();
      t.future.shouldNotBeCompleted();
      p.complete(F.unit);
      t.future.shouldNotBeCompleted();
      t.sendSignal();
      t.future.shouldBeCompleted(F.unit);
    }

    [Test]
    public void NotCompletedThenSignalThenCompletion() {
      var t = Future.async<Unit>(out var p).delayUntilSignal();
      t.future.shouldNotBeCompleted();
      t.sendSignal();
      t.future.shouldNotBeCompleted();
      p.complete(F.unit);
      t.future.shouldBeCompleted(F.unit);
    }

    [Test]
    public void CompletedThenSignal() {
      var t = Future.successful(F.unit).delayUntilSignal();
      t.future.shouldNotBeCompleted();
      t.sendSignal();
      t.future.shouldBeCompleted(F.unit);
    }
  }

  public class FutureTestToRxVal {
    [Test]
    public void WithUnknownType() {
      var f = Future.async(out Promise<int> promise);
      var rx = f.toRxVal();
      rx.value.shouldBeNone();
      promise.complete(10);
      rx.value.shouldBeSome(10);
    }

    [Test]
    public void WithRxValInside() {
      Promise<IRxVal<int>> p;
      var f = Future.async(out p);
      var rx = f.toRxVal(0);
      rx.value.shouldEqual(0);
      var rx2 = RxRef.a(100);
      p.complete(rx2);
      rx.value.shouldEqual(100);
      rx2.value = 200;
      rx.value.shouldEqual(200);
    }
  }

  public class FutureTestTimeout {
    Promise<int> promise;
    Future<int> sourceFuture;
    TestTimeContext tc;

    static readonly TimeSpan t = TimeSpan.FromMilliseconds(100);
    static readonly Duration d = t;

    [SetUp]
    public void setup() {
      sourceFuture = Future.async(out promise);
      tc = new TestTimeContext();
    }

    [Test]
    public void WhenSourceCompletes() {
      var f = sourceFuture.timeout(d, tc);
      f.value.shouldBeNone();
      tc.timePassed = d - new Duration(1);
      f.value.shouldBeNone();
      promise.complete(5);
      f.value.shouldBeSome(Either<TimeSpan, int>.Right(5));
    }

    [Test]
    public void WhenSourceCompletesOnFailure() {
      var f = sourceFuture.timeout(d, tc);
      var failureResult = new List<TimeSpan>();
      f.onFailure(failureResult.Add);
      f.value.shouldBeNone();
      tc.timePassed = d - new Duration(1);
      f.value.shouldBeNone();
      promise.complete(5);
      f.value.shouldBeSome(Either<TimeSpan, int>.Right(5));
      tc.timePassed += t;
      failureResult.shouldEqualEnum();
    }

    [Test]
    public void WhenSourceDelaysOnFailure() {
      var f = sourceFuture.timeout(d, tc);
      var failureResult = new List<TimeSpan>();
      f.onFailure(failureResult.Add);
      f.value.shouldBeNone();
      tc.timePassed = d;
      f.value.shouldBeSome(F.left<TimeSpan, int>(d));
      tc.timePassed += t;
      failureResult.shouldEqualEnum(t);
    }
  }

  public class OptionFutureTestExtract {
    [Test]
    public void WhenNone() => F.none<Future<int>>().extract().shouldBeOfUnfulfilledType();

    [Test]
    public void WhenSome() {
      var f = Future.successful(3);
      Some.a(f).extract().shouldEqual(f);
    }
  }

  public class OptionFutureTestExtractOpt {
    [Test]
    public void WhenNone() =>
      F.none<Future<int>>().extractOpt().shouldBeOfSuccessfulType(F.none<int>());

    [Test]
    public void WhenSome() =>
      Some.a(Future.successful(3)).extractOpt().shouldEqual(Future.successful(Some.a(3)));
  }
}