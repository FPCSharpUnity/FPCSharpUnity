﻿using TMPro;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class TextMeshPro_Color : SerializedTweener<Color, TextMeshProUGUI> {
    public TextMeshPro_Color() : base(
      TweenOps.color, SerializedTweenerOps.Add.color, SerializedTweenerOps.Extract.tmProColor,
      TweenMutatorsU.tmProColor, Defaults.color
    ) { }
  }
}