using System.Collections.Generic;
using FPCSharpUnity.core.pools;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.gradient {
  public abstract class ModifyVerticesUI : BaseMeshEffect {
    public abstract void ModifyVertices(List<UIVertex> vertexList);

    public override void ModifyMesh(VertexHelper vh) {
      if (!IsActive()) return;
      using var verts = ListPool<UIVertex>.instance.BorrowDisposable();
      vh.GetUIVertexStream(verts);
      ModifyVertices(verts);  // calls the old ModifyVertices which was used on pre 5.2
      vh.Clear();
      vh.AddUIVertexTriangleStream(verts);
    }
    
    public enum UVType {
      UV0, UV1
    }

    public static void setUVAsFullRect(List<UIVertex> vertexList, UVType type) {
      var count = vertexList.Count;
      if (count == 0 ) return;

      var minX = vertexList[0].position.x;
      var minY = vertexList[0].position.y;
      var maxX = minX;
      var maxY = minY;
      
      for (var i = 0; i < vertexList.Count; i++) {
        if (minX > vertexList[i].position.x) minX = vertexList[i].position.x;
        if (maxX < vertexList[i].position.x) maxX = vertexList[i].position.x;
        if (minY > vertexList[i].position.y) minY = vertexList[i].position.y;
        if (maxY < vertexList[i].position.y) maxY = vertexList[i].position.y;
      }

      // Do this check only once on the outer loop for better performance.
      if (type == UVType.UV0) {
        for (var i = 0; i < count; i++) {
          var uiVertex = vertexList[i];
          uiVertex.uv0 = new Vector2(
            Mathf.InverseLerp(minX, maxX, uiVertex.position.x),
            Mathf.InverseLerp(minY, maxY, uiVertex.position.y)
          );
          vertexList[i] = uiVertex;
        }
      }
      else {
        for (var i = 0; i < count; i++) {
          var uiVertex = vertexList[i];
          uiVertex.uv1 = new Vector2(
            Mathf.InverseLerp(minX, maxX, uiVertex.position.x),
            Mathf.InverseLerp(minY, maxY, uiVertex.position.y)
          );
          vertexList[i] = uiVertex;
        }
      }
    }
  }
}