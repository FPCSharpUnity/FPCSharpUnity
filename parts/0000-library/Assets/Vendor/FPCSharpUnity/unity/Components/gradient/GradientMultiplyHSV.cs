using UnityEngine;
using System.Collections.Generic;
using FPCSharpUnity.unity.Extensions;

namespace FPCSharpUnity.unity.Components.gradient {
  [AddComponentMenu("UI/Effects/Gradient Multiply HSV")]
  public class GradientMultiplyHSV : GradientBase {
#pragma warning disable 649
    [SerializeField] Color topColor = Color.white, bottomColor = Color.black;
#pragma warning restore 649

    public override void ModifyVertices(List<UIVertex> vertexList) {
      var c1 = topColor.RGBToHSV();
      var c2 = bottomColor.RGBToHSV();
      GradientHelper.modifyVertices(
        vertexList, (c1, c2), static (tpl, c, t) => {
          var a = ((Color) c).RGBToHSV();
          var b = Color.Lerp(tpl.c2, tpl.c1, t);
          return new Color(a.r + b.r, a.g * b.g, a.b * b.b).HSVToRGB();
        },
        type
      );
    }
  }
}