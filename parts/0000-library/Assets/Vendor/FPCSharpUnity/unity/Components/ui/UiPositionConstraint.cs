using ExhaustiveMatching;
using FPCSharpUnity.unity.Components.Interfaces;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components.ui;
[
  TypeInfoBox(
    "Constraints RectTransform position on a chosen axis to be lined with the position of another RectTransform. " 
    + "Also provides a way to limit the range of the constrained position."
  ),
  ExecuteInEditMode
]
public class UiPositionConstraint : UIBehaviour, IMB_Update {
  
#pragma warning disable 649
  // ReSharper disable NotNullMemberIsNotInitialized
  [SerializeField, NotNull] RectTransform _copyPositionFrom;
  [SerializeField, OnValueChanged(nameof(setupDrivenRectTransformTracker))] RectTransform.Axis _constrainedAxis;
  [SerializeField] float _multiplier = 1;
  [SerializeField] float _minPosition;
  [SerializeField] float _maxPosition;
  // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
  
  RectTransform rectTransform;

  DrivenRectTransformTracker rtTracker;

  public void Update() => updatePosition();

  protected override void OnEnable() {
    rectTransform = GetComponent<RectTransform>();
    setupDrivenRectTransformTracker();
    updatePosition();
  }

  protected override void OnDisable() => rtTracker.Clear();

  void updatePosition() {
    if (!_copyPositionFrom) return;
    var positionToMove = rectTransform.anchoredPosition;
    var referencePosition = _copyPositionFrom.anchoredPosition;
    var constrainedPosition = Mathf.Clamp(
      (_constrainedAxis == RectTransform.Axis.Horizontal ? referencePosition.x : referencePosition.y) * _multiplier, 
      _minPosition,
      _maxPosition
    );
    if (_constrainedAxis == RectTransform.Axis.Horizontal) {
      positionToMove.x = constrainedPosition;
    } else {
      positionToMove.y = constrainedPosition;
    }
    rectTransform.anchoredPosition = positionToMove;
  }
  
  /// <summary> Disables editing of position fields on the inspector. </summary>
  void setupDrivenRectTransformTracker() {
    rtTracker.Clear();
    rtTracker.Add(
      this, 
      rectTransform, 
      _constrainedAxis switch {
        RectTransform.Axis.Horizontal => DrivenTransformProperties.AnchoredPositionX,
        RectTransform.Axis.Vertical => DrivenTransformProperties.AnchoredPositionY,
        _ => throw ExhaustiveMatch.Failed(_constrainedAxis)
      }
    );
  }
}