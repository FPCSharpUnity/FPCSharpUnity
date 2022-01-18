using GenerationAttributes;

namespace FPCSharpUnity.unity.Data {
  [Record] public partial struct TypedKey<Type> {
    public readonly string key;
  }
}