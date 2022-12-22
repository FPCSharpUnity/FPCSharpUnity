using System;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class FloatExts {
    public static int roundToInt(this float number) => (int) Math.Round(number);

    [PublicAPI] public static int toIntClamped(this float number) {
      if (number > int.MaxValue) return int.MaxValue;
      if (number < int.MinValue) return int.MinValue;
      return (int) number;
    }

    [PublicAPI] public static byte roundToByteClamped(this float number) {
      if (number > byte.MaxValue) return byte.MaxValue;
      if (number < byte.MinValue) return byte.MinValue;
      return (byte) Math.Round(number);
    }

    /// <summary>
    /// Checks whether the value approximately equals 0 (a wrapper for <see cref="Mathf.Approximately"/>).
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool approx0(this float number) => Mathf.Approximately(number, 0);

    /// <summary>
    /// Computes 1/x for a value and returns 0 if the value approximately equals 0.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="approx0"/> to figure out whether the specified value equals zero.
    /// </remarks>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float recipOr0(this float value) => value.approx0() ? 0.0f : 1 / value;
    
    /// <summary>Figures out whether the value is NaN.</summary>
    /// <remarks>Uses <see cref="float.IsNaN"/> under the hood.</remarks>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isNaN(this float value) => float.IsNaN(value);

    /// <returns>Unit length vector that represents supplied angle</returns>
    public static Vector2 radiansToVector(this float angleRadians) => 
      new(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
  }
}