using FPCSharpUnity.core.macros;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Asset path relative to Unity project directory.</summary>
  [Record(RecordType.ConstructorOnly), NewTypeImplicitTo] public readonly partial struct AssetPath {
    public readonly string path;
  }
}