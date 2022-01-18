using GenerationAttributes;
using FPCSharpUnity.core.typeclasses;

namespace FPCSharpUnity.unity.Data.scenes {
  [Record] public readonly partial struct SceneName : IStr {
    public readonly string name;

    public string asString() => name;

    public static implicit operator string(SceneName s) => s.name;
  }
}