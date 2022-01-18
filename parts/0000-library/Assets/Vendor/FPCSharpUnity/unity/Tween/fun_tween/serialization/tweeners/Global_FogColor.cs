using FPCSharpUnity.unity.Tween.fun_tween.serialization.targets;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class Global_FogColor : SerializedTweener<Color, GlobalTweenTargets> {
    public Global_FogColor() : base(
      TweenOps.color, SerializedTweenerOps.Add.color, SerializedTweenerOps.Extract.globalFogColor, 
      GlobalTweenTargets.globalFogColor, Defaults.color
    ) { }
  }
}