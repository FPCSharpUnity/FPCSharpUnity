using FPCSharpUnity.core.functional;
using GenerationAttributes;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  /// <summary>Allows you to represent a runtime value.</summary>
  [Record] public readonly partial struct ForRepresentation {
    /// <summary>None if this value is available statically.</summary>
    public readonly Option<object> objectReference;
    public readonly string name;
    public readonly IRenderableValue value;
  }
}