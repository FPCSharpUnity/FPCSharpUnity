﻿using System;
using System.Collections.Generic;
using System.IO;
using FPCSharpUnity.core.exts;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.serialization;
using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.Filesystem {
  [
    Serializable, PublicAPI, Record(ConstructorFlags.None, GenerateToString = false)
  ]
  public partial struct PathStr : IComparable<PathStr>, IStr, IDebugStr {
    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
    [SerializeField] string _path;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

    #endregion

    public string path => _path;

    public PathStr(string path) {
      _path = path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
    }
    public static PathStr a(string path) => new PathStr(path);

    #region Comparable

    public int CompareTo(PathStr other) => string.Compare(path, other.path, StringComparison.Ordinal);

    sealed class PathRelationalComparer : Comparer<PathStr> {
      public override int Compare(PathStr x, PathStr y) {
        return string.Compare(x.path, y.path, StringComparison.Ordinal);
      }
    }

    public static Comparer<PathStr> pathComparer { get; } = new PathRelationalComparer();

    #endregion

    public static PathStr operator /(PathStr s1, string s2) => new PathStr(Path.Combine(s1.path, s2));
    public static implicit operator string(PathStr s) => s.path;

    public PathStr dirname => new PathStr(Path.GetDirectoryName(path));
    public PathStr basename => new PathStr(Path.GetFileName(path));
    public string extension => Path.GetExtension(path);
    
    /// <summary>Removes a single file extension from the path.</summary>
    public PathStr withoutExtension => a(_path.Substring(0, _path.Length - extension.Length));

    /// <summary>Removes all file extensions from the path.</summary>
    public PathStr withoutExtensions {
      get {
        // TODO: optimize
        var current = this;
        while (true) {
          var updated = current.withoutExtension;
          
          if (current == updated) return current;
          else current = updated;
        }
      }
    }

    public PathStr ensureBeginsWith(PathStr p) => path.StartsWithFast(p.path) ? this : p / path;
    public override string ToString() => asString();
    public readonly string asString() => _path;
    public string asDebugString() => $"{nameof(Path)}({_path})";

    /// <summary>Path in UNIX format (with / slashes).</summary>
    public string unixString => ToString().Replace('\\', '/');

    /// <summary>
    /// Use this with Unity Resources, AssetDatabase and PrefabUtility methods
    /// </summary>
    public string unityPath => Path.DirectorySeparatorChar == '/' ? path : path.Replace('\\' , '/');
    
    public PathStr toAbsolute => a(Path.GetFullPath(path));
    
#if UNITY_EDITOR
    /// <summary>Relative directory to the `Unity Assets` folder (E.g. `relative_path/unity/Assets/`).</summary>
    public static readonly PathStr unityAssetsDirectory = a(Application.dataPath);
    
    /// <summary>Relative directory to the `Unity Project` folder (E.g. `relative_path/unity/`).</summary>
    public static readonly PathStr unityProjectDirectory = a(Application.dataPath) / "..";
#endif
    
    public static readonly ISerializedRW<PathStr> serializedRW =
      SerializedRW.str.mapNoFail(s => new PathStr(s), path => path.path);

    public bool startsWith(PathStr path, bool ignoreCase=false) => _path.StartsWithFast(path._path, ignoreCase);
    public bool endsWith(PathStr path, bool ignoreCase=false) => _path.EndsWithFast(path._path, ignoreCase);
  }

  public static class PathStrExts {
    static Option<PathStr> onCondition(this string s, bool condition) =>
      (condition && s != null).opt(new PathStr(s));

    public static Option<PathStr> asFile(this string s) => s.onCondition(File.Exists(s));
    public static Option<PathStr> asDirectory(this string s) => s.onCondition(Directory.Exists(s));
  }
}
