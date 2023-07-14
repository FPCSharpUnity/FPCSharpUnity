using System.IO;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Filesystem {
  public static class PathStrExts {
    public static PathStr toAbsoluteRelativeToProjectDir(this PathStr path) {
      var full = path.path.Contains(':') || path.path.StartsWithFast("/")
        ? path
        : Application.dataPath + "/../" + path;

      return new PathStr(Path.GetFullPath(full));
    }

    /// <summary>
    /// Ensures that a directory exits for a given path.
    /// <para/>
    /// If a directory does not exist it will get created.
    /// </summary>
    public static void ensureDirectory(this PathStr path) {
      if (!Directory.Exists(path)) {
        Directory.CreateDirectory(path);
      }
    }
  }
}