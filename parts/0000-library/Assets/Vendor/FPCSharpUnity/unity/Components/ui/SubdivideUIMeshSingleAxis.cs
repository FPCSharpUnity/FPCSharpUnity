using System.Collections.Generic;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.pools;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  /// <summary>
  /// Similar to <see cref="SubdivideUIMesh"/>, but this class generates significantly less vertexes if we need to
  /// subdivide only on a single axis.
  ///
  /// For example if <see cref="subdivideCount"/> is 3 and <see cref="axis"/> is <see cref="Axis.Horizontal"/> then it
  /// would look like this: https://monosnap.com/file/Km6kRd3DPlAAPMrtr8qKLwyI7IHuuY
  /// </summary>
  public class SubdivideUIMeshSingleAxis : BaseMeshEffect {
    public enum Axis {
      Horizontal = 0,
      Vertical = 1
    }
    
    public uint subdivideCount = 1;
    public Axis axis;

    public override void ModifyMesh(VertexHelper vh) {
      if (!IsActive()) return;
      if (subdivideCount == 0) return;

      var pool = ListPool<UIVertex>.instance;
      using var vertsOld = pool.BorrowDisposable();
      using var vertsUpdated = pool.BorrowDisposable();
      vh.GetUIVertexStream(vertsOld);
      
      subdivide(vertsOld, vertsUpdated, subdivideCount, axis);
      
      vh.Clear();
      vh.AddUIVertexTriangleStream(vertsUpdated);
    }

    static void subdivide(List<UIVertex> oldList, List<UIVertex> newList, uint subdivideCount, Axis axis) {
      for (var i = 0; i < oldList.Count; i += 3) {
        var c1 = oldList[i];
        var c2 = oldList[i+1];
        var c3 = oldList[i+2];

        var filled = testAndFill(c1, c2, c3) || testAndFill(c2, c3, c1) || testAndFill(c3, c1, c2);
        if (!filled) {
          newList.Add(c1); newList.Add(c2); newList.Add(c3);
        }

        bool testAndFill(in UIVertex a, in UIVertex b, in UIVertex c) {
          switch (axis) {
            case Axis.Horizontal:
              if (!Mathf.Approximately(a.position.x, b.position.x)) return false;
              break;
            case Axis.Vertical:
              if (!Mathf.Approximately(a.position.y, b.position.y)) return false;
              break;
          }

          var edgeA = a;
          var edgeB = b;

          for (var j = 1; j < subdivideCount; j++) {
            var lerpValue = j / (float) subdivideCount;
            var middleA = a.lerp(c, lerpValue);
            var middleB = b.lerp(c, lerpValue);
            newList.Add(edgeA); newList.Add(edgeB); newList.Add(middleB);
            newList.Add(edgeA); newList.Add(middleB); newList.Add(middleA);
            edgeA = middleA;
            edgeB = middleB;
          }
          newList.Add(edgeA); newList.Add(edgeB); newList.Add(c);
          return true;
        }
      }
    }
  }
}
