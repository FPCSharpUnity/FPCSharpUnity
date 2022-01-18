﻿using System.Collections.Generic;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;
using FPCSharpUnity.core.exts;

namespace FPCSharpUnity.unity.Extensions {
  [TestFixture]
  public class IDictionaryExtsTestGetOrUpdate {
    [Test]
    public void Test() {
      var dictionary = new Dictionary<int,string>();
      dictionary.getOrUpdate(1, () => "one").shouldEqual("one");
      dictionary.getOrUpdate(1, () => "two").shouldEqual("one");
    }
  }
}