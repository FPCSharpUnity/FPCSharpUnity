using System;
using GenerationAttributes;
using FPCSharpUnity.core.config;
using FPCSharpUnity.core.json;
using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.Data {
  [Serializable, Record]
  public partial struct Percentage {
    [SerializeField, Range(0, 1), PublicAccessor] float _value;

    public static readonly Config.Parser<JsonValue, Percentage> parser = Config.floatParser.map(f => new Percentage(f));

    public string asString() => Str.s(Mathf.RoundToInt(_value)) + "%";

    public static Percentage oneHundred => new(1f);
    public static Percentage zero => new(0f);

    public static Percentage operator +(Percentage a, Percentage b) => new (a._value + b._value);
    public static Percentage operator -(Percentage a, Percentage b) => new (a._value - b._value);
    public static Percentage operator /(Percentage a, float b) => new (a._value / b);
    public static Percentage operator *(Percentage a, float b) => new (a._value * b);
  }
}