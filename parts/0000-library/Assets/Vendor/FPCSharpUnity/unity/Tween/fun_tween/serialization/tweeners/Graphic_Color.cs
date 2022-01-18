﻿using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class Graphic_Color : SerializedTweener<Color, Graphic> {
    public Graphic_Color() : base(
      TweenOps.color, SerializedTweenerOps.Add.color, SerializedTweenerOps.Extract.graphicColor,
      TweenMutatorsU.graphicColor, Defaults.color
    ) { }
  }
}