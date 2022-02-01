using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Net;
using NUnit.Framework;
using FPCSharpUnity.core.test_framework;

namespace FPCSharpUnity.unity.Test.Net {
  public class QueryStringTestParseKV {
    [Test]
    public void WhenNoEncodedParams() {
      QueryString.parseKV("foo=bar&bar=baz").shouldEqual(ImmutableList.Create(
        Tpl.a("foo", "bar"), Tpl.a("bar", "baz")
      ));
    }

    [Test]
    public void WhenEncodedParams() {
      QueryString.parseKV("f%20oo=b%20ar&bar=baz").shouldEqual(ImmutableList.Create(
        Tpl.a("f oo", "b ar"), Tpl.a("bar", "baz")
      ));
    }

    [Test]
    public void WhenPartiallyNoValue() {
      QueryString.parseKV("f%20oo&bar=baz").shouldEqual(ImmutableList.Create(
        Tpl.a("f oo", ""), Tpl.a("bar", "baz")
      ));
    }

    [Test]
    public void WhenNotKV() {
      QueryString.parseKV("foo").shouldEqual(ImmutableList.Create(Tpl.a("foo", "")));
    }

    [Test]
    public void WhenEmpty() {
      QueryString.parseKV("").shouldEqual(ImmutableList<Tpl<string, string>>.Empty);
    }
  }
}