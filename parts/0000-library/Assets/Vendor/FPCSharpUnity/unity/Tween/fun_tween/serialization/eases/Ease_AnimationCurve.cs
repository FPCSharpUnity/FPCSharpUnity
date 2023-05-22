using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Tween.fun_tween.serialization.eases {
  [AddComponentMenu("")]
  public class Ease_AnimationCurve : ComplexSerializedEase {
    // [SerializeField, NotNull] AnimationCurve _curve;
    
    protected override Ease createEase() => Eases.linear;
    public override string easeName => nameof(AnimationCurve);
  }
}