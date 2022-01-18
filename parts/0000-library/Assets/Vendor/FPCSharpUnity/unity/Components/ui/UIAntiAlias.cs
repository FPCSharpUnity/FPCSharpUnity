using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.pools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  [TypeInfoBox(
    "Extends the image mesh to provide a basic anti-alias effect on the rectangle mesh edges."
  )]
  public class UIAntiAlias : BaseMeshEffect {
    [SerializeField] float _offset = .5f;

    [LazyProperty] static ILog log => Log.d.withScope(nameof(UIAntiAlias));

    public override void ModifyMesh(VertexHelper vh) {
      if (!IsActive()) return;

      using var vertsDisposable = ListPool<UIVertex>.instance.BorrowDisposable();
      var verts = vertsDisposable.value;
      
      vh.GetUIVertexStream(verts);

      if (verts.Count < 6) {
        log.error($"Should contain vat least 6 vertexes, but found {verts.Count}");
        return;
      }

      var maxX = verts[0].position.x;
      var maxY = verts[0].position.y;
      var minX = maxX;
      var minY = maxY;
      
      // Find rectangle bounds.
      for (var i = 0; i < verts.Count; i++) {
        var pos = verts[i].position;
        if (pos.x > maxX) maxX = pos.x;
        if (pos.y > maxY) maxY = pos.y;
        if (pos.x < minX) minX = pos.x;
        if (pos.y < minY) minY = pos.y;
      }

      for (var i = 0; i < verts.Count; i += 3) {
        var area = MathUtils.crossProductFrom3Points(verts[i].position, verts[i + 1].position, verts[i + 2].position);
        // Ignore degenerate triangles.
        if (Mathf.Abs(area) < 1e-4) continue;
        
        // Check each segment of a triangle.
        trySegment(i, i + 1);
        trySegment(i + 1, i + 2);
        trySegment(i + 2, i);
      }
      
      void trySegment(int idx1, int idx2) {
        // When segment is vertical or horizontal and on bounds line, then generate the extended mesh edge.

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (verts[idx1].position.x == verts[idx2].position.x) {
          if (verts[idx1].position.x == minX) {
            addToSegment2(idx1, idx2, new Vector3(-_offset, 0));
          }
          if (verts[idx1].position.x == maxX) {
            addToSegment2(idx1, idx2, new Vector3(_offset, 0));
          }
        }
        if (verts[idx1].position.y == verts[idx2].position.y) {
          if (verts[idx1].position.y == minY) {
            addToSegment2(idx1, idx2, new Vector3(0, -_offset));
          }
          if (verts[idx1].position.y == maxY) {
            addToSegment2(idx1, idx2, new Vector3(0, _offset));
          }
        }
        // ReSharper restore CompareOfFloatsByEqualityOperator
      }

      void addToSegment2(int idx1, int idx2, Vector3 offsetVector) {
        var v1 = verts[idx1];
        var v2 = verts[idx2];

        // Alpha goes to 0 on new vertices to simulate anti-aliasing.
        var v1New = v1;
        v1New.position += offsetVector;
        v1New.color = v1New.color.with32Alpha(0);
        
        var v2New = v2;
        v2New.position += offsetVector;
        v2New.color = v2New.color.with32Alpha(0);

        {
          // Non-allocating version of vh.AddUIVertexQuad
          var startIndex = vh.currentVertCount;
          vh.AddVert(v2);
          vh.AddVert(v1);
          vh.AddVert(v1New);
          vh.AddVert(v2New);

          vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
          vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
      }
    }
  }
}
