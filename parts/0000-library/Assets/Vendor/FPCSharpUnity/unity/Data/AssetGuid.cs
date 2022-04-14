using FPCSharpUnity.core.macros;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Asset GUID obtained from Unity asset database.</summary>
  [Record(RecordType.ConstructorOnly), NewTypeImplicitTo] public readonly partial struct AssetGuid {
    public readonly string guid;
  }
}