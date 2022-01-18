using Sirenix.OdinInspector;
using FPCSharpUnity.core.exts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPCSharpUnity.unity.Components.ui {

  public class SimpleLayout : UIBehaviour {
    enum Alignment : byte { Start = 0, Middle = 1, End = 2 }

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] RectTransform.Axis _axis;
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] float _marginFront, _marginBack, _spacing = 10;
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] Alignment _alignment;
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] bool _useRectSizeForAlignment;
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] bool _useItemScaleWhenEvaluatingRectSize;
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] Alignment _secondaryAxisAlignment = Alignment.Middle;
    [SerializeField, OnValueChanged(nameof(recalculateLayout))] bool _resizeParent;
    [SerializeField] Transform[] _childrenToExclude = new Transform[0];
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    public float spacing {
      get => _spacing;
      set {
        _spacing = value;
        recalculateLayout();
      }
    }

    [Button]
    public void recalculateLayout() {
      var rt = (RectTransform) transform;
      var totalItems = 0;
      var sumOfItemSizeAlongMainAxis = 0f;

      for (var i = 0; i < rt.childCount; i++) {
        var child = rt.GetChild(i);
        if (!child.gameObject.activeSelf || _childrenToExclude.contains(child)) continue;
        totalItems++;
        if (_useRectSizeForAlignment) sumOfItemSizeAlongMainAxis += getSize(child).mainAxis;
      }

      var totalLength = 
        _marginFront + (Mathf.Max(0, totalItems - 1)) * _spacing + _marginBack + sumOfItemSizeAlongMainAxis;
      var offset =
        _alignment switch {
          Alignment.End => -totalLength,
          Alignment.Middle => (-totalLength / 2f),
          _ => 0
        };

      var lastItemEnd = _marginFront;
      for (var i = 0; i < rt.childCount; i++) {
        var child = rt.GetChild(i);
        if (!child.gameObject.activeSelf || _childrenToExclude.contains(child)) continue;
        var childRT = (RectTransform) child.transform;
        var pos = new Vector2();
        var currentItemSize = getSize(child);
        var mainAxisSize = _useRectSizeForAlignment ? currentItemSize.mainAxis : 0f;
        var currentOffset = lastItemEnd + (mainAxisSize * .5f) + offset;
        lastItemEnd += mainAxisSize + spacing;
        if (_axis == RectTransform.Axis.Horizontal) {
          pos.x = currentOffset;
          pos.y = _secondaryAxisAlignment switch {
            Alignment.Start => -currentItemSize.secondaryAxis * .5f,
            Alignment.Middle => 0,
            Alignment.End => currentItemSize.secondaryAxis * .5f,
            _ => throw _secondaryAxisAlignment.argumentOutOfRange()
          };
        }
        else {
          pos.x = _secondaryAxisAlignment switch {
            Alignment.Start => currentItemSize.secondaryAxis * .5f,
            Alignment.Middle => 0,
            Alignment.End => -currentItemSize.secondaryAxis * .5f,
            _ => throw _secondaryAxisAlignment.argumentOutOfRange()
          };
          pos.y = -currentOffset;
        }
        childRT.anchoredPosition = pos;
      }
      if (_resizeParent) {
        rt.SetSizeWithCurrentAnchors(
          _axis,
          totalLength
        );
      }

      (float mainAxis, float secondaryAxis) getSize(Transform elementTransform) {
        var rt = (RectTransform) elementTransform;
        var result = _useItemScaleWhenEvaluatingRectSize 
          ? rt.rect.size * elementTransform.localScale
          : rt.rect.size;
        return _axis == RectTransform.Axis.Horizontal ? (result.x, result.y) : (result.y, result.x);
      }
    }
  }
}