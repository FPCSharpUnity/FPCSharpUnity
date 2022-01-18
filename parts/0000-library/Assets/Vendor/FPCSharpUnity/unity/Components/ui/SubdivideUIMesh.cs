using System.Collections.Generic;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.pools;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  public class SubdivideUIMesh : BaseMeshEffect {
    public uint subdivideCount = 1;

    public override void ModifyMesh(VertexHelper vh) {
      if (!IsActive()) return;
      if (subdivideCount == 0) return;

      var pool = ListPool<UIVertex>.instance;
      using var vertsOldDisposable = pool.BorrowDisposable();
      var vertsOld = vertsOldDisposable.value;
      using var vertsUpdatedDisposable = pool.BorrowDisposable();
      var vertsUpdated = vertsUpdatedDisposable.value;
      
      vh.GetUIVertexStream(vertsOld);

      // Subdivide first time.
      subdivideOnce(vertsOld, vertsUpdated);
      // Repeat subdivisions.
      for (var i = 1; i < subdivideCount; i++) {
        (vertsOld, vertsUpdated) = (vertsUpdated, vertsOld);
        vertsUpdated.Clear();
        subdivideOnce(vertsOld, vertsUpdated);
      }

      vh.Clear();
      vh.AddUIVertexTriangleStream(vertsUpdated);
    }

    static void subdivideOnce(List<UIVertex> oldList, List<UIVertex> newList) {
      for (var i = 0; i < oldList.Count; i += 3) {
        var c1 = oldList[i];
        var c2 = oldList[i+1];
        var c3 = oldList[i+2];
        var m1 = c1.lerp(c2, .5f);
        var m2 = c2.lerp(c3, .5f);
        var m3 = c3.lerp(c1, .5f);
        newList.Add(c1); newList.Add(m1); newList.Add(m3);
        newList.Add(c2); newList.Add(m2); newList.Add(m1);
        newList.Add(c3); newList.Add(m3); newList.Add(m2);
        newList.Add(m1); newList.Add(m2); newList.Add(m3);
      }
    }
  }
}
