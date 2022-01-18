using FPCSharpUnity.unity.Tween.fun_tween.serialization.targets;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners {
  [AddComponentMenu("")]
  public class Global_FogDensity : SerializedTweener<float, GlobalTweenTargets> {
    public Global_FogDensity() : base(
      TweenOps.float_, SerializedTweenerOps.Add.float_, SerializedTweenerOps.Extract.globalFogDensity, 
      GlobalTweenTargets.globalFogDensity, Defaults.float_
    ) { }
  }
}