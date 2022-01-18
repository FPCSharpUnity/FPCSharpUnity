﻿using System;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Path to an asset path.</summary>
  public readonly struct AssetPath : IEquatable<AssetPath> {
    public readonly string path;
    public AssetPath(string path) { this.path = path; }

    #region Equality

    public bool Equals(AssetPath other) {
      return string.Equals(path, other.path);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is AssetPath && Equals((AssetPath) obj);
    }

    public override int GetHashCode() {
      return (path != null ? path.GetHashCode() : 0);
    }

    public static bool operator ==(AssetPath left, AssetPath right) { return left.Equals(right); }
    public static bool operator !=(AssetPath left, AssetPath right) { return !left.Equals(right); }

    #endregion

    public override string ToString() => $"{nameof(AssetPath)}({path})";
    public static implicit operator string(AssetPath path) => path.path;
  }
}