using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.targets {
  [CreateAssetMenu(menuName = "FunTween/Global Tween Targets")]
  public class GlobalTweenTargets : ScriptableObject {
    [PublicAPI] public static readonly TweenMutator<Color, GlobalTweenTargets>
      globalFogColor = (value, _, relative) => TweenMutatorsU.globalFogColor(value, F.unit, relative);
    
    [PublicAPI] public static readonly TweenMutator<float, GlobalTweenTargets>
      globalFogDensity = (value, _, relative) => TweenMutatorsU.globalFogDensity(value, F.unit, relative);
  }
}