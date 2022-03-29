﻿using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {

  [PublicAPI]
  public static class VectorExts {
    public static Vector2 withX(this Vector2 v, float x) => new Vector2(x, v.y);
    public static Vector2 withY(this Vector2 v, float y) => new Vector2(v.x, y);

    public static Vector2 addX(this Vector2 v, float x) => new Vector2(v.x + x, v.y);
    public static Vector2 addY(this Vector2 v, float y) => new Vector2(v.x, v.y + y);
    
    public static Vector2 multiplyX(this Vector2 v, float x) => new Vector2(v.x * x, v.y);
    public static Vector2 multiplyY(this Vector2 v, float y) => new Vector2(v.x, v.y * y);

    public static Vector2 multiply(this Vector2 v, Vector2 v2) => new Vector2(v.x * v2.x, v.y * v2.y);
    public static Vector2 divide(this Vector2 v, Vector2 v2) {
      float div(float a1, float a2) =>
        a2 == 0
          ? a1 < 0
            ? float.MinValue
            : float.MaxValue
          : a1 / a2;
      return new Vector2(div(v.x, v2.x), div(v.y, v2.y));
    }

    public static Vector3 withX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
    public static Vector3 withY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    public static Vector3 withZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

    public static Vector3 addX(this Vector3 v, float x) => new Vector3(v.x + x, v.y, v.z);
    public static Vector3 addY(this Vector3 v, float y) => new Vector3(v.x, v.y + y, v.z);
    public static Vector3 addZ(this Vector3 v, float z) => new Vector3(v.x, v.y, v.z + z);
    
    public static Vector3 multiplyX(this Vector3 v, float x) => new Vector3(v.x * x, v.y, v.z);
    public static Vector3 multiplyY(this Vector3 v, float y) => new Vector3(v.x, v.y * y, v.z);
    public static Vector3 multiplyZ(this Vector3 v, float z) => new Vector3(v.x, v.y, v.z * z);

    public static Vector3 multiply(this Vector3 v, Vector3 v2) => new Vector3(v.x * v2.x, v.y * v2.y, v.z * v2.z);
    public static Vector3 divide(this Vector3 v, Vector3 v2) => new Vector3(v.x / v2.x, v.y / v2.y, v.z / v2.z);

    static string logFormat(float f) => $"{f,10:0.000}";
    public static string logFormat(this Vector2 v) => $"({logFormat(v.x)},{logFormat(v.y)})";
    public static string logFormat(this Vector3 v) => $"({logFormat(v.x)},{logFormat(v.y)},{logFormat(v.z)})";

    public static Vector2 with2(
      this Vector2 v,
      Option<float> x = default,
      Option<float> y = default
    ) {
      Option.ensureValue(ref x);
      Option.ensureValue(ref y);
      return new Vector3(x.getOrElse(v.x), y.getOrElse(v.y));
    }

    public static Vector3 with3(
      this Vector3 v,
      Option<float> x = default,
      Option<float> y = default,
      Option<float> z = default
    ) {
      Option.ensureValue(ref x);
      Option.ensureValue(ref y);
      Option.ensureValue(ref z);
      return new Vector3(x.getOrElse(v.x), y.getOrElse(v.y), z.getOrElse(v.z));
    }

    public static Vector2 rotate90(this Vector2 v) => new Vector2(-v.y, v.x);
    public static Vector2 rotate180(this Vector2 v) => new Vector2(-v.x, -v.y);
    public static Vector2 rotate270(this Vector2 v) => new Vector2(v.y, -v.x);

    public static float cross(this Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    public static Vector2 rotate(this Vector2 v, float degrees) {
      var radians = degrees * Mathf.Deg2Rad;
      var sin = Mathf.Sin(radians);
      var cos = Mathf.Cos(radians);

      var tx = v.x;
      var ty = v.y;

      return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    public static float segmentAngle(this Vector2 from, Vector2 to) =>
      Mathf.Atan2(to.y - from.y, to.x - from.x);
    public static float segmentAngle(this Vector3 from, Vector3 to) =>
      Mathf.Atan2(to.y - from.y, to.x - from.x);
      
    public static float signedAngle(Vector2 from, Vector2 to) {
      var degrees = Vector2.Angle(from, to);
      return from.cross(to) < 0 ? -degrees : degrees;
    }

    public static float distanceSquared(this Vector2 a, Vector2 b) {
      var dx = a.x - b.x;
      var dy = a.y - b.y;
      return dx * dx + dy * dy;
    }

    public static bool approximately(this Vector2 a, Vector2 b) => 
      Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);

    public static bool approximately(this Vector3 a, Vector3 b) => 
      Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);

    /// <returns>Vector angle in radians</returns>
    public static float atan2(this Vector2 v) => Mathf.Atan2(v.y, v.x);

    public static Quaternion directionTo2dRotation(this Vector2 v) {
#pragma warning disable 618
      // Quaternion.EulerAngles uses radians
      return Quaternion.EulerAngles(0, 0, v.atan2());
#pragma warning restore 618
    }
  }
}
