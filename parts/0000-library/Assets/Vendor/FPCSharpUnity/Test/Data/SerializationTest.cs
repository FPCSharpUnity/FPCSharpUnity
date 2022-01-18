using System;
using FPCSharpUnity.unity.Functional;
using NUnit.Framework;
using FPCSharpUnity.core.serialization;
using FPCSharpUnity.core.test.serialization;

namespace FPCSharpUnity.unity.Data {
  public class SerializationTestTplRW : SerializationTestBase {
    static readonly ISerializedRW<Tpl<int, string>> rw =
      SerializedRW.integer.tpl(SerializedRW.str);

    [Test] public void TestTpl() {
      var t = F.t(1, "foo");
      var serialized = rw.serializeToArray(t);
      checkWithNoise(rw, serialized, t);
    }
  }
}