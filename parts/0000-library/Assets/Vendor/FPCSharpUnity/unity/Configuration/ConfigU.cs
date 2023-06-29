using FPCSharpUnity.unity.Data;
using JetBrains.Annotations;
using FPCSharpUnity.core.config;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.json;
using UnityEngine;
using static FPCSharpUnity.core.config.Config;

namespace FPCSharpUnity.unity.Configuration {
  [PublicAPI] public class ConfigU {
    public static readonly Parser<JsonValue, Range> iRangeParser =
      rangeParser(intParser, (l, u) => new Range(l, u));

    public static readonly Parser<JsonValue, FRange> fRangeParser =
      rangeParser(floatParser, (l, u) => new FRange(l, u));

    public static readonly Parser<JsonValue, URange> uRangeParser =
      rangeParser(uintParser, (l, u) => new URange(l, u));
    
    public static readonly Parser<JsonValue, Color> colorParser =
      stringParser.flatMap(s => ColorUtility.TryParseHtmlString(s, out var c) ? Some.a(c) : None._);
    
    /// <summary>
    /// Parses a <see cref="Color"/> from {"r": 0.0, "g": 0.0, "b": 0.0, "a": 0.0} JSON.
    /// </summary>
    public static readonly Parser<JsonValue, Color> colorRGBAParser =
      configParser.flatMapTry((_, cfg) => new Color(
        r: cfg.getFloat("r"),
        g: cfg.getFloat("g"),
        b: cfg.getFloat("b"),
        a: cfg.getFloat("a")
      ));
  }
}