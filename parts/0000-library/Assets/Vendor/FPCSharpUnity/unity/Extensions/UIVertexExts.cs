using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI]
  public static class UIVertexExts {
    public static UIVertex lerp(this in UIVertex a, in UIVertex b, float t) =>
      new UIVertex {
        position = Vector3.LerpUnclamped(a.position, b.position, t),
        normal = Vector3.LerpUnclamped(a.normal, b.normal, t),
        color = Color32.LerpUnclamped(a.color, b.color, t),
        tangent = Vector3.LerpUnclamped(a.tangent, b.tangent, t),
        uv0 = Vector3.LerpUnclamped(a.uv0, b.uv0, t),
        uv1 = Vector3.LerpUnclamped(a.uv1, b.uv1, t),
        uv2 = Vector3.LerpUnclamped(a.uv2, b.uv2, t),
        uv3 = Vector3.LerpUnclamped(a.uv3, b.uv3, t)
      };
  }
}
