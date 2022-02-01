using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.typeclasses;

namespace FPCSharpUnity.unity.Net {
  public class QueryString : IStr {
    static readonly char[] equalsSeparator = {'='};

    readonly StringBuilder sb = new StringBuilder();
    bool isFirst = true;

    public void append(string key, string value) {
      if (isFirst) isFirst = false;
      else sb.Append("&");
      sb.Append(Uri.EscapeDataString(key));
      sb.Append("=");
      sb.Append(Uri.EscapeDataString(value));
    }

    public string queryString => sb.ToString();
    public string asString() => queryString;

    public override string ToString() => $"{nameof(QueryString)}({queryString})";

    public static QueryString build<A>(
      IEnumerable<A> qsParams, Func<A, string> extractKey, Func<A, string> extractValue
    ) {
      var qs = new QueryString();
      foreach (var a in qsParams)
        qs.append(extractKey(a), extractValue(a));
      return qs;
    }

    public static QueryString build(IEnumerable<KeyValuePair<string, string>> qsParams) =>
      build(qsParams, _ => _.Key, _ => _.Value);

    public static QueryString build(IEnumerable<Tpl<string, string>> qsParams) =>
      build(qsParams, _ => _._1, _ => _._2);

    public static ImmutableList<Tpl<string, string>> parseKV(string str) {
      if (str.isEmpty()) return ImmutableList<Tpl<string, string>>.Empty;

      var parts = str.Split('&');
      var list = parts.Select(part => {
        var kv = part.Split(equalsSeparator, 2);
        var key = Uri.UnescapeDataString(kv[0]);
        var value = kv.Length == 2 ? Uri.UnescapeDataString(kv[1]) : "";
        return Tpl.a(key, value);
      }).ToImmutableList();
      return list;
    }
  }
}
