﻿
using UnityEngine;
using UnityEngine.Serialization;

namespace FPCSharpUnity.unity.Components.aspect_ratio {
  // [Help(
  //   HelpType.Warning, HelpPosition.Before,
  //   "We have no idea what this script does. It was written by Tadas. If you figure out " +
  //   "what this script does, come tell us at the developer room. Thanks."
  // )]
  public sealed class CanvasAspectRatioScalerDown : CanvasAspectRatioScaler {
    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
    [SerializeField, FormerlySerializedAs("scaleGradiantStrength")]
      float scaleGradientStrength = 0.9f;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

    #endregion

    protected override void OnRectTransformDimensionsChange(){
      base.OnRectTransformDimensionsChange();
      target.localScale *= target.localScale.y < 1 ? scaleGradientStrength : 1;
    }
  }
}