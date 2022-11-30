﻿using UnityEngine;
using System.Collections.Generic;
using FPCSharpUnity.unity.Extensions;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace FPCSharpUnity.unity.Components.gradient {
  [AddComponentMenu("UI/Effects/Gradient")]
  public partial class GradientSimple : GradientBase {
    [SerializeField, PublicAccessor] Color32 topColor = Color.white, bottomColor = Color.black;

    public override void ModifyVertices(List<UIVertex> vertexList) {
      GradientHelper.modifyVertices(
        vertexList, (bottomColor, topColor),
        static (tpl, c, t) => Color32.Lerp(tpl.bottomColor, tpl.topColor, t), type
      );
    }

    [Button] void swapColors() {
      (topColor, bottomColor) = (bottomColor, topColor);
    }

    [PublicAPI]
    public void setAlpha(float alpha) {
      var alpha32 = Mathf.Lerp(0, 255, alpha).roundToByteClamped();
      topColor.a = alpha32;
      bottomColor.a = alpha32;
      if (graphic != null) graphic.SetVerticesDirty();
    }

    [PublicAPI]
    public void setColor(Color top, Color bottom) {
      topColor = top;
      bottomColor = bottom;
      if (graphic != null) graphic.SetVerticesDirty();
    }
  }
}