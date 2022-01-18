using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class QuaternionExts {
    [PublicAPI] public static float toRotation2D(this Quaternion q) => q.eulerAngles.z * Mathf.Deg2Rad;
  }
}