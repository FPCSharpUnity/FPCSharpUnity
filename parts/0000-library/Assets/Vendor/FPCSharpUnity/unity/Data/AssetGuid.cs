using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.typeclasses;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Asset GUID obtained from Unity asset database.</summary>
  [Record(ConstructorFlags.Constructor), NewTypeImplicitTo] 
  public readonly partial struct AssetGuid : IStr {
    public readonly string guid;

    public string asString() => guid;
  }
}