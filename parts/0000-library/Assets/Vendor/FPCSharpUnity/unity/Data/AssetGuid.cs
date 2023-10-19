using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.typeclasses;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Asset GUID obtained from Unity asset database.</summary>
  [Record(ConstructorFlags.Constructor), NewTypeImplicitTo] 
  public readonly partial struct AssetGuid : IStr {
    public readonly string guid;

    public string asString() => guid;

    
    /// <summary>
    /// Return true if the guid represents a built-in asset.
    /// </summary>
    public bool isBuiltInAsset() =>
      guid 
        // BuiltInExtra
        is "0000000000000000f000000000000000"
        // EditorResource
        or "0000000000000000e000000000000000"
        // DefaultResource
        or "0000000000000000d000000000000000";
  }
}