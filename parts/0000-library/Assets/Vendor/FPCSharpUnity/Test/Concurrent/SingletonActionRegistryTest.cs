using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.test_framework.spec;

namespace FPCSharpUnity.unity.Concurrent {
  public class SingletonActionRegistryTest : ImplicitSpecification {
    [Test]
    public void specification() => describe(() => {
      var registry = new SingletonActionRegistry<int>();

      when["future is going to complete in the future"] = () => {
        var f1 = let(() => Lazy.a(() => 10));

        it["should not call the action until future is completed"] = () => {
          var called = false;
          registry[f1.value] = _ => called = true;
          called.shouldBeFalse();
        };

        it["should only call the last registered action"] = () => {
          var result = 0;
          registry[f1.value] = x => result = x;
          registry[f1.value] = x => result = x * 2;
          f1.value.strict.forSideEffects();
          result.shouldEqual(f1.value.strict * 2);
        };

        it["should not interfere between registered futures of same type"] = () => {
          var f2 = Lazy.a(() => 20);
          var result1 = 0;
          var result2 = 0;
          registry[f1.value] = x => result1 = 0;
          registry[f1.value] = x => result1 = x;
          registry[f2] = x => result2 = 0;
          registry[f2] = x => result2 = x;
          f1.value.strict.forSideEffects();
          f2.strict.forSideEffects();
          Tpl.a(result1, result2).shouldEqual(Tpl.a(f1.value.strict, f2.strict));
        };
      };

      when["future is already completed"] = () => {
        const int VALUE = 10;
        var f1 = Lazy.value(VALUE);

        it["should immediately call the given action every time it is called"] = () => {
          var result = 0;
          registry[f1] = x => result = x;
          result.shouldEqual(VALUE, "1st time");
          registry[f1] = x => result = x * 2;
          result.shouldEqual(VALUE * 2, "2nd time");
        };
      };
    });
  }
}