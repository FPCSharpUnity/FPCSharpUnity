using System.IO;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.test_framework;
using FPCSharpUnity.core.test_framework.spec;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Filesystem {
  public class PathStrTest : ImplicitSpecification {
    [Test] public void withoutExtension() => describe(() => {
      when["it's a directory"] = () => {
        it["should be kept the same"] =
          () => PathStr.a("foo/").withoutExtension.shouldEqual(PathStr.a("foo/"));
      };
      
      when["file no extensions"] = () => {
        it["should be kept the same"] =
          () => PathStr.a("foo/bar").withoutExtension.shouldEqual(PathStr.a("foo/bar"));
      };

      when["file has one extension"] = () => {
        it["should remove a single extension"] =
          () => PathStr.a("foo/bar.exe").withoutExtension.shouldEqual(PathStr.a("foo/bar"));
      };

      when["file has two extensions"] = () => {
        it["should remove a single extension"] =
          () => PathStr.a("foo/bar.dll.exe").withoutExtension.shouldEqual(PathStr.a("foo/bar.dll"));
      };
    });
    
    [Test] public void withoutExtensions() => describe(() => {
      when["it's a directory"] = () => {
        it["should be kept the same"] =
          () => PathStr.a("foo/").withoutExtensions.shouldEqual(PathStr.a("foo/"));
      };
      
      when["file no extensions"] = () => {
        it["should be kept the same"] =
          () => PathStr.a("foo/bar").withoutExtensions.shouldEqual(PathStr.a("foo/bar"));
      };

      when["file has one extension"] = () => {
        it["should remove a single extension"] =
          () => PathStr.a("foo/bar.exe").withoutExtensions.shouldEqual(PathStr.a("foo/bar"));
      };

      when["file has two extensions"] = () => {
        it["should remove all extensions"] =
          () => PathStr.a("foo/bar.dll.exe").withoutExtensions.shouldEqual(PathStr.a("foo/bar"));
      };
    });
  }
  
  class PathStrTestConstructor {
    [Test]
    public void ItShouldNormalizeDirectorySeparators() {
      var expected = Path.Combine(Path.Combine("foo", "bar"), "baz");
      new PathStr("foo/bar/baz").path.shouldEqual(expected);
      new PathStr("foo\\bar\\baz").path.shouldEqual(expected);
    }
  }

  class PathStrTestEnsureBeginsWith {
    [Test]
    public void WhenDoesNotBegin() {
      new PathStr("foo/bar/baz")
        .ensureBeginsWith(new PathStr("lol/php")).shouldEqual(new PathStr("lol/php/foo/bar/baz"));
    }

    [Test]
    public void WhenItBeginsWith() {
      new PathStr("foo/bar/baz")
        .ensureBeginsWith(new PathStr("foo/bar")).shouldEqual(new PathStr("foo/bar/baz"));
    }
  }
}
