﻿using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Functional {
  public class OptionTestEquality {
    [Test]
    public void WhenEqualNone() {
      F.none<int>().shouldEqual(None._);
      F.none<string>().shouldEqual(None._);
    }

    [Test]
    public void WhenEqualSome() {
      Some.a(3).shouldEqual(Some.a(3));
      Some.a(5).shouldEqual(Some.a(5));
      Some.a("foo").shouldEqual(Some.a("foo"));
    }

    [Test]
    public void WhenNotEqual() {
      ImmutableList.Create(
        F.none<int>(),
        Some.a(0),
        Some.a(1)
      ).shouldTestInequalityAgainst(ImmutableList.Create(
        Some.a(10),
        Some.a(11)
      ));
    }
  }

  public class OptionTestAsEnumDowncast {
    class A {}

    class B : A {}

    [Test]
    public void WhenSome() {
      var b = new B();
      new Option<B>(b).asEnumerable().shouldEqual(b.yield<A>());
    }

    [Test]
    public void WhenNone() {
      Option<A>.None.asEnumerable().shouldEqual(Enumerable.Empty<A>());
    }
  }

  public class OptionTestAsEnum {
    [Test]
    public void WhenSome() {
      Some.a(3).asEnumerable().shouldEqual(3.yield());
    }

    [Test]
    public void WhenOnlyA() {
      F.none<int>().asEnumerable().shouldEqual(Enumerable.Empty<int>());
    }
  }

  public class OptionTestFoldFunctionFunction {
    [Test]
    public void WhenSomeGood() {
      new Option<string>("this is a set option").
        fold(() => false, op => op == "this is a set option").
        shouldBeTrue();
    }

    [Test]
    public void WhenNone() {
      Option<string>.None.
        fold(() => false, op => op == "this is a set option").
        shouldBeFalse();
    }

    [Test]
    public void WhenSomeDiffer() {
      new Option<string>("This is").
        fold(() => false, op => op == "this is a set option").
        shouldBeFalse();
    }
  }

  public class OptionTestFoldValueFunction {
    [Test]
    public void WhenSomeGood() {
      new Option<string>("this is a set option").
        fold(false, op => op == "this is a set option").
        shouldBeTrue();
    }

    [Test]
    public void WhenNone() {
      Option<string>.None.
        fold(false, op => op == "this is a set option").
        shouldBeFalse();
    }

    [Test]
    public void WhenSomeDiffer() {
      new Option<string>("This is").
        fold(false, op => op == "this is a set option").
        shouldBeFalse();
    }
  }

  public class OptionTestFoldValueValue {
    [Test]
    public void WhenSomeGood() {
      new Option<string>("this is a set option").
        fold(false, true).
        shouldBeTrue();
    }

    [Test]
    public void WhenNone() {
      Option<string>.None.
        fold(false, true).
        shouldBeFalse();
    }

    [Test]
    public void WhenSomeDiffer() {
      new Option<string>("This is").
        fold(false, true).
        shouldBeTrue();
    }
  }

  public class OptionTestGetOrElseFunction {
    [Test]
    public void WhenSome() {
      new Option<bool>(true).getOrElse(() => false).shouldBeTrue();
    }

    [Test]
    public void WhenNone() {
      Option<bool>.None.getOrElse(() => false).shouldBeFalse();
    }
  }

  public class OptionTestGetOrElseValue {
    [Test]
    public void WhenSome() {
      new Option<bool>(true).getOrElse(false).shouldBeTrue();
    }

    [Test]
    public void WhenNone() {
      Option<bool>.None.getOrElse(false).shouldBeFalse();
    }
  }

  public class OptionTestGetOrNull {
    [Test]
    public void WhenSome() {
      new Option<string>("stuff").getOrNull().shouldEqual("stuff");
    }
    [Test]
    public void WhenNone() {
      Option<string>.None.getOrNull().shouldEqual(null);
    }
  }

  public class OptionEnumeratorTest {
    [Test]
    public void WhenSome() {
      var test = new Option<int>(5);
      var ran = false;
      foreach (var number in test) {
        number.shouldEqual(5);
        ran = true;
      }
      ran.shouldBeTrue();
    }

    [Test]
    public void WhenNone() {
      var test = Option<int>.None;
      var ran = false;
      foreach (var _ in test) ran = true;
      ran.shouldBeFalse();
    }
  }

  public class OptionTestOperatorTrue {
    [Test]
    public void WhenTrue() {
      var wasTrue = false;
      if (Some.a(3)) wasTrue = true;
      wasTrue.shouldBeTrue();
    }

    [Test]
    public void WhenFalse() {
      var wasTrue = false;
      if (F.none<int>()) wasTrue = true;
      wasTrue.shouldBeFalse();
    }
  }

  public class OptionTestOperatorOr {
    [Test]
    public void WhenSome() {
      var option = new Option<string>("this is a set option");
      (option || new Option<string>("if it was not set now it is")).shouldEqual(option);
    }

    [Test]
    public void WhenNone() {
      var option = None._;
      var newOption = new Option<string>("if it was not set now it is");
      (option || newOption).shouldEqual(newOption);
    }
  }
}