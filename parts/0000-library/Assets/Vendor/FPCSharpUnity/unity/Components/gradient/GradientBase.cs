using UnityEngine;

namespace FPCSharpUnity.unity.Components.gradient {
  public abstract class GradientBase : ModifyVerticesUI {
    #region Unity Serialized Fields

#pragma warning disable 649
    [SerializeField] protected GradientHelper.GradientType type = GradientHelper.GradientType.Vertical;
#pragma warning restore 649

    #endregion
  }
}