using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.pools;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  public class Skew : BaseMeshEffect {
    public float xSkew, ySkew;

    public override void ModifyMesh(VertexHelper vh) {
      if (!IsActive()) return;
      if (xSkew.approx0() && ySkew.approx0()) return;

      using var vertsDisposable = ListPool<UIVertex>.instance.BorrowDisposable();
      var verts = vertsDisposable.value;
      vh.GetUIVertexStream(verts);
      for (var i = 0; i < verts.Count; i++) {
        var vert = verts[i];
        var pos = vert.position;
        vert.position = new Vector3(pos.x + pos.y * xSkew, pos.y + pos.x * ySkew, pos.z);
        verts[i] = vert;
      }
      vh.Clear();
      vh.AddUIVertexTriangleStream(verts);
    }
  }
}
