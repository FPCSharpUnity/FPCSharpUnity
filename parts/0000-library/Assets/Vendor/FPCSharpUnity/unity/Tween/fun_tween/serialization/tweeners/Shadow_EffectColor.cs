﻿using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class Shadow_EffectColor : SerializedTweener<Color, Shadow> {
    public Shadow_EffectColor() : base(
      TweenOps.color, SerializedTweenerOps.Add.color, SerializedTweenerOps.Extract.shadowEffectColor, 
      TweenMutatorsU.shadowEffectColor, Defaults.color
    ) { }
  }
}