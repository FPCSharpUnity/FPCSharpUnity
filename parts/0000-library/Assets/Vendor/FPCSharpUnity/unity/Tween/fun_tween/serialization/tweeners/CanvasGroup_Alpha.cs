using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class CanvasGroup_Alpha : SerializedTweener<float, CanvasGroup> {
    public CanvasGroup_Alpha() : base(
      TweenOps.float_, SerializedTweenerOps.Add.float_, SerializedTweenerOps.Extract.canvasGroupAlpha,
      TweenMutatorsU.canvasGroupAlpha, Defaults.float_
    ) { }
  }
}