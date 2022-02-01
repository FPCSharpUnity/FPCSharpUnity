using System.Linq;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Functional;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Collection {
  [TestFixture]
  public class CarouselEmitterTest {
    [Test]
    public void TestOneElement() {
      var ce = new CarouselEmitter<string>(new[] {Tpl.a("a", 1)});
      var actual = ce.Take(5).ToArray();
      var expected = new[] {"a", "a", "a", "a", "a"};
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void TestTwoElements() {
      var ce = new CarouselEmitter<string>(new[] {Tpl.a("a", 1), Tpl.a("b", 1)});
      var actual = ce.Take(5).ToArray();
      var expected = new[] {"a", "b", "a", "b", "a"};
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void MaxCount1() {
      var ce = new CarouselEmitter<string>(new[] { Tpl.a("a", 1), Tpl.a("b", 1) });
      Assert.AreEqual(1, ce.maxCount);
    }

    [Test]
    public void MaxCount2() {
      var ce = new CarouselEmitter<string>(new[] {Tpl.a("a", 1), Tpl.a("b", 3)});
      Assert.AreEqual(3, ce.maxCount);
    }

    [Test]
    public void TotalCount1() {
      var ce = new CarouselEmitter<string>(new[] { Tpl.a("a", 1), Tpl.a("b", 1) });
      Assert.AreEqual(2, ce.totalCount);
    }

    [Test]
    public void TotalCount2() {
      var ce = new CarouselEmitter<string>(new[] {Tpl.a("a", 1), Tpl.a("b", 3)});
      Assert.AreEqual(4, ce.totalCount);
    }

    [Test]
    public void TestDifferentWeightsElements() {
      var ce = new CarouselEmitter<string>(new[] {Tpl.a("a", 1), Tpl.a("b", 3)});

      var actual = ce.Take(5).ToArray();
      var expected = new[] { "a", "b", "b", "b", "a" };
      Assert.AreEqual(expected, actual);
    }

    [Test]
    public void TestMultiples() {
      var ce = new CarouselEmitter<string>(
        new[] { Tpl.a("a", 1), Tpl.a("b", 3), Tpl.a("a", 2) }
      );

      var actual = ce.Take(12).ToArray();
      var expected = new[] {
        "a", "b", "a",
        "b", "a",
        "b",
        "a", "b", "a",
        "b", "a",
        "b"
      };
      Assert.AreEqual(expected, actual);
    }
  }
}
