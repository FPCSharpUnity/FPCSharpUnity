using System;
using UnityEngine;
using System.Collections.Generic;
using ExhaustiveMatching;

namespace FPCSharpUnity.unity.Components.gradient {
  public static class GradientHelper {
    public enum GradientType { Vertical, Horizontal }

    public static void modifyVertices<Data>(
      List<UIVertex> vertexList, Data data, Func<Data, Color32, float, Color32> f, GradientType type
    ) {
      switch (type) {
        case GradientType.Vertical:
          modifyVertices(vertexList, data, f, v => v.y);
          break;
        case GradientType.Horizontal:
          modifyVertices(vertexList, data, f, v => v.x);
          break;
        default:
          throw ExhaustiveMatch.Failed(type);
      }
    }

    static void modifyVertices<Data>(
      List<UIVertex> vertexList, Data data, Func<Data, Color32, float, Color32> f, Func<Vector3, float> getAxisFn
    ) {
      var count = vertexList.Count;
      if (count == 0) return;
      var min = getAxisFn(vertexList[0].position);
      var max = getAxisFn(vertexList[0].position);

      for (var i = 1; i < count; i++) {
        var current = getAxisFn(vertexList[i].position);
        if (current > max) {
          max = current;
        }
        else if (current < min) {
          min = current;
        }
      }

      var uiElementHeight = max - min;

      for (var i = 0; i < count; i++) {
        var uiVertex = vertexList[i];
        uiVertex.color = f(data, uiVertex.color, (getAxisFn(uiVertex.position) - min) / uiElementHeight);
        vertexList[i] = uiVertex;
      }
    }
  }
}