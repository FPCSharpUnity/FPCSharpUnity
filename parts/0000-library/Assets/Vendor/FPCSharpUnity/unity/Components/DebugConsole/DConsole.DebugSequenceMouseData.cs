using System.Collections.Immutable;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  [LazyProperty] public static DebugSequenceMouseData DEFAULT_MOUSE_DATA => new();
  
  [LazyProperty] public static ImmutableList<int> DEFAULT_MOUSE_SEQUENCE =>
    ImmutableList.Create(0, 1, 3, 2, 0, 2, 3, 1, 0);
  
  public sealed class DebugSequenceMouseData {
    public readonly int width, height;
    public readonly ImmutableList<int> sequence;

    public DebugSequenceMouseData(int width=2, int height=2, ImmutableList<int> sequence=null) {
      this.width = width;
      this.height = height;
      this.sequence = sequence ?? DEFAULT_MOUSE_SEQUENCE;
    }
  }
}