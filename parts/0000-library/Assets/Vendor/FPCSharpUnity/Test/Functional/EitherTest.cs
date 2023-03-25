﻿using System.Collections.Immutable;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Functional {
  public class EitherTestEquality {
    [Test]
    public void WhenLeftEquals() {
      Either<int, string>.Left(0).shouldEqual(Either<int, string>.Left(0));
      Either<int, string>.Left(10).shouldEqual(Either<int, string>.Left(10));
    }

    [Test]
    public void WhenRightEquals() {
      Either<int, string>.Right("0").shouldEqual(Either<int, string>.Right("0"));
      Either<int, string>.Right("10").shouldEqual(Either<int, string>.Right("10"));
    }

    [Test]
    public void WhenNotEqual() {
      ImmutableList.Create(
        Either<int, string>.Left(0),
        Either<int, string>.Left(1),
        Either<int, string>.Right("0"),
        Either<int, string>.Right("1")
      ).shouldTestInequalityAgainst(ImmutableList.Create(
        Either<int, string>.Left(10),
        Either<int, string>.Left(11),
        Either<int, string>.Right("10"),
        Either<int, string>.Right("11")
      ));
    }
  }

  public class EitherTestSequenceValidations {
    [Test]
    public void WhenHasOneError() {
      var l = ImmutableList.Create("error");
      new[] {
        Either<ImmutableList<string>, int>.Left(l),
        Either<ImmutableList<string>, int>.Right(4)
      }.sequenceValidations().shouldBeLeftEnum(l);
    }

    [Test]
    public void WhenHasMultipleErrors() {
      new[] {
        Either<ImmutableList<string>, int>.Left(ImmutableList.Create("error")),
        Either<ImmutableList<string>, int>.Left(ImmutableList.Create("error2"))
      }.sequenceValidations().shouldBeLeftEnum(ImmutableList.Create("error", "error2"));
    }

    [Test]
    public void WhenHasNoErrors() {
      new[] {
        Either<ImmutableList<string>, int>.Right(3),
        Either<ImmutableList<string>, int>.Right(4)
      }.sequenceValidations().shouldBeRightEnum(ImmutableList.Create(3, 4));
    }
  }

  public class EitherTestIsLeft {
    [Test] public void WhenLeft() => new Either<int, string>(3).isLeft.shouldBeTrue();
    [Test] public void WhenRight() => new Either<int, string>("3").isLeft.shouldBeFalse();
  }

  public class EitherTestIsRight {
    [Test] public void WhenLeft() => new Either<int, string>(3).isRight.shouldBeFalse();
    [Test] public void WhenRight() => new Either<int, string>("3").isRight.shouldBeTrue();
  }

  public class EitherTestLeftValue {
    [Test] public void WhenLeft() => new Either<int, string>(3).leftValue.shouldBeSome(3);
    [Test] public void WhenRight() => new Either<int, string>("3").leftValue.shouldBeNone();
  }

  public class EitherTestUnsafeGetLeft {
    [Test] public void WhenLeft() => new Either<int, string>(3).__unsafeGetLeft.shouldEqual(3);
    [Test] public void WhenRight() => new Either<int, string>("3").__unsafeGetLeft.shouldEqual(default(int));
  }

  public class EitherTestRightValue {
    [Test] public void WhenLeft() => new Either<int, string>(3).rightValue.shouldBeNone();
    [Test] public void WhenRight() => new Either<int, string>("3").rightValue.shouldBeSome("3");
  }

  public class EitherTestUnsafeGetRight {
    [Test] public void WhenLeft() => new Either<int, string>(3).__unsafeGetRight.shouldEqual(default(string));
    [Test] public void WhenRight() => new Either<int, string>("3").__unsafeGetRight.shouldEqual("3");
  }

  public class EitherTestLeftOrThrow {
    [Test] public void WhenLeft() => new Either<int, string>(3).leftOrThrow.shouldEqual(3);
    [Test] public void WhenRight() => Assert.Throws<WrongEitherSideException>(
      () => { var _ = new Either<int, string>("3").leftOrThrow; }
    );
  }

  public class EitherTestRightOrThrow {
    [Test] public void WhenLeft() => Assert.Throws<WrongEitherSideException>(
      () => { var _ = new Either<int, string>(3).rightOrThrow; }
    );
    [Test] public void WhenRight() => new Either<int, string>("3").rightOrThrow.shouldEqual("3");
  }

  public class EitherTestToString {
    [Test] public void WhenLeft() => new Either<int, string>(3).ToString().shouldEqual("Left(3)");
    [Test] public void WhenRight() => new Either<int, string>("foo").ToString().shouldEqual("Right(foo)");
  }

  public class EitherTestFlatMapLeft {
    [Test] public void WhenLeftToLeft() =>
      new Either<int, string>(3)
      .flatMapLeftM(i => new Either<char,string>(i.ToString()[0]))
      .shouldBeLeft('3');

    [Test] public void WhenLeftToRight() =>
      new Either<int, string>(3)
      .flatMapLeftM(i => new Either<char,string>(i.ToString()))
      .shouldBeRight("3");

    [Test] public void WhenRight() =>
      new Either<int, string>("3")
      .flatMapLeftM(i => new Either<char,string>('a'))
      .shouldBeRight("3");
  }

  public class EitherTestFlatMapRight {
    [Test]
    public void WhenRightToLeft() =>
      new Either<int, string>("3")
      .flatMapRightM(s => new Either<int, char>(s.parseInt().rightOrThrow))
      .shouldBeLeft(3);

    [Test]
    public void WhenRightToRight() =>
      new Either<int, string>("3")
      .flatMapRightM(s => new Either<int, char>(s[0]))
      .shouldBeRight('3');

    [Test]
    public void WhenLeft() =>
      new Either<int, string>(3)
      .flatMapRightM(s => new Either<int, char>('a'))
      .shouldBeLeft(3);
  }

  public class EitherTestMapLeft {
    static char mapper(int i) => i.ToString()[0];

    [Test] public void WhenLeft() => new Either<int, string>(3).mapLeftM(mapper).shouldBeLeft('3');
    [Test] public void WhenRight() => new Either<int, string>("foo").mapLeftM(mapper).shouldBeRight("foo");
  }

  public class EitherTestMapRight {
    static char mapper(string s) => s[0];

    [Test] public void WhenLeft() => new Either<int, string>(3).mapRightM(mapper).shouldBeLeft(3);
    [Test] public void WhenRight() => new Either<int, string>("foo").mapRightM(mapper).shouldBeRight("f");
  }

  public class EitherTestFold {
    static char folder(int i) => i.ToString()[0];
    static char folder(string s) => s[0];

    [Test] public void WhenLeft() => Either<int, string>.Left(3).foldM(folder, folder).shouldEqual('3');
    [Test] public void WhenRight() => Either<int, string>.Right("foo").foldM(folder, folder).shouldEqual('f');
  }

  public class EitherTestVoidFold {
    static void test(Either<int, string> e, char expected) {
      var result = Option<char>.None;
      void leftFolder(int i) => result = i.ToString()[0].some();
      void rightFolder(string s) => result = s[0].some();
      e.voidFoldM(leftFolder, rightFolder);
      result.shouldBeSome(expected);
    }

    [Test] public void WhenLeft() => test(Either<int, string>.Left(3), '3');
    [Test] public void WhenRight() => test(Either<int, string>.Right("foo"), 'f');
  }

  public class EitherTestForeach {
    [Test]
    public void WhenLeft() {
      foreach (var _ in Either<int, string>.Left(3))
        Assert.Fail("It should not iterate if left");
    }

    [Test]
    public void WhenRight() {
      var called = 0;
      foreach (var b in Either<string, int>.Right(3)) {
        b.shouldEqual(3);
        called++;
      }
      called.shouldEqual(1, "it should yield once");
    }
  }
}