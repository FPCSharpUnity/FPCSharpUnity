using FPCSharpUnity.core.test_framework;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Data {
  public class UrlTestSlashOperator {
    [Test]
    public void WhenEndsWithSlash() =>
      (new Url("foo/") / "bar").shouldEqual(new Url("foo/bar"));

    [Test]
    public void WhenDoesNotEndWithSlash() =>
      (new Url("foo") / "bar").shouldEqual(new Url("foo/bar"));
  }
}