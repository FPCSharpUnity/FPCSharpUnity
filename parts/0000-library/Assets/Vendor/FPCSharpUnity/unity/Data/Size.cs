using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Data; 

[Record(GenerateToString = false, GenerateComparer = true, GenerateGetHashCode = true)] public readonly partial struct Size {
  public readonly int width, height;

  public override string ToString() { return $"{nameof(Size)}[{width}x{height}]"; }
  
  public Vector2Int vector2Int => new(width, height);

  public float aspect => height == 0 ? 1 : (float) width / height;
}