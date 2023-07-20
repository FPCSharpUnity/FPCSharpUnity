using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Data;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  public enum Direction { Left, Up, Right, Down }
  
  [Record, NewTypeStringWrapper]
  public readonly partial struct GroupName {
    public readonly string name;
  }
  
  public enum DebugSequenceInvocationMethod : byte {
    /// <summary>Invoked by clicking down in regions with the mouse.</summary>
    Mouse,
    
    /// <summary>Invoked by providing a specified sequence via Unity Input axis API.</summary>
    UnityInputAxisDirections,
    
    /// <summary>Invoked by pressing a specified <see cref="KeyCodeWithModifiers"/>.</summary>
    Keyboard,
  }
}