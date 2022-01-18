﻿using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class Image_FillAmount : SerializedTweener<float, Image> {
    public Image_FillAmount() : base(
      TweenOps.float_, SerializedTweenerOps.Add.float_, SerializedTweenerOps.Extract.imageFillAmount,
      TweenMutatorsU.imageFillAmount, Defaults.float_
    ) { }
  }
}