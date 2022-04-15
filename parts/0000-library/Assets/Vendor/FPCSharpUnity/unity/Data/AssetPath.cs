using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.typeclasses;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Asset path relative to Unity project directory.</summary>
  [Record(RecordType.ConstructorOnly), NewTypeImplicitTo] public readonly partial struct AssetPath : IStr {
    public readonly string path;

    public string asString() => path;
  }
}