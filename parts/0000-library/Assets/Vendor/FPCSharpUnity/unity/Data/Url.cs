﻿using System;
using FPCSharpUnity.core.config;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.json;
using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.Data {
  /** Stupid tag on string. Because System.Uri is heavy. */
  [Serializable]
  public struct Url : IStr, IEquatable<Url> {
    #region Unity Serialized Fields

#pragma warning disable 649
    [SerializeField, NotNull] string _url;
#pragma warning restore 649

    #endregion

    public string url => _url;

    public Url(string url) { _url = url; }
    public static Url a(string url) => new Url(url);

    public override string ToString() => $"{nameof(Url)}({url})";

    #region Equality

    public bool Equals(Url other) {
      return string.Equals(url, other.url);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is Url && Equals((Url) obj);
    }

    public override int GetHashCode() {
      return (url != null ? url.GetHashCode() : 0);
    }

    public static bool operator ==(Url left, Url right) { return left.Equals(right); }
    public static bool operator !=(Url left, Url right) { return !left.Equals(right); }

    #endregion

    public string asString() => url;

    public static implicit operator string(Url url) => url.asString();

    public static Url operator +(Url u1, Url u2) => new Url(u1.url + u2.url);

    public static Url operator /(Url u1, string u2) {
      var lastIsSlash = u1.url.lastChar().existsM(_ => _ == '/');
      return new Url(u1.url + (lastIsSlash ? "" : "/") + u2);
    }
  }

  public static class UrlExts {
    public static Url toUrl(this Uri uri) => new Url(uri.ToString());
    
    public static readonly Config.Parser<JsonValue, Url> parser = Config.stringParser.map(str => new Url(str));

    public static readonly Config.Parser<JsonValue, Option<Url>> parserOpt = Config.opt(parser);
  }
}