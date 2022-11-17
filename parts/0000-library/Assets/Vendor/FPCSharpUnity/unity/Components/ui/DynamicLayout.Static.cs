using System.Collections.Generic;
using ExhaustiveMatching;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.Utilities;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  public static class Init {
    const float EPS = 1e-9f;
      
    /*/// <summary>Apply method for <see cref="Init{TData,TView}"/> constructor.</summary>
    public static Init<TData, TView> a<TData, TView>(
      DynamicLayout backing,
      IEnumerable<TData> layoutData,
      ITracker dt,
      TView viewExampleForTypeInference,
      bool renderLatestItemsFirst = false
    ) where TData : IElementData<TView> where TView : IElementView => new Init<TData, TView>(
      backing, layoutData, dt, renderLatestItemsFirst: renderLatestItemsFirst
    );
      
    /// <summary>Apply method for <see cref="Init{TData,TView}"/> constructor.</summary>
    public static Init<TData, TView> a<TData, TView>(
      RectTransform _container, RectTransform _maskRect,
      IEnumerable<TData> layoutData,
      bool isHorizontal, Padding padding, float spacingInScrollableAxis,
      ITracker tracker,
      TView viewExampleForTypeInference,
      bool renderLatestItemsFirst = false,
      ExpandElementsRectSizeInSecondaryAxis expandElements = ExpandElementsRectSizeInSecondaryAxis.DontExpand
    ) where TData : IElementData<TView> where TView : IElementView => new Init<TData, TView>(
      _container, _maskRect, layoutData, isHorizontal: isHorizontal, padding, 
      spacingInScrollableAxis: spacingInScrollableAxis, tracker, renderLatestItemsFirst: renderLatestItemsFirst, 
      expandElements
    );*/

    /// <summary>
    /// Calculates all positions for <see cref="iElementDatas"/> and invokes a callback
    /// <see cref="forEachElementAction"/> on each of them.
    /// <para/>
    /// Returns `None` if the iteration was stopped early, as it's impossible to calculate the result then.
    /// </summary>
    public static Option<ForEachElementResult> forEachElementStoppable<TElementData, Data>(
      float spacing, IReadOnlyList<TElementData> iElementDatas, 
      bool renderLatestItemsFirst, Padding padding, bool isHorizontal, 
      RectTransform containersRectTransform, Rect visibleRect, Data dataA, 
      ForEachElementActionStoppable<TElementData, Data> forEachElementAction
    ) where TElementData : IElement {
      var containerRect = containersRectTransform.rect;
      var containerHeight = containerRect.height;
      var containerWidth = containerRect.width;

      // Depending on orientation it's top or left
      float paddingPercentageStart;
      // Depending on orientation it's bottom or right
      float paddingPercentageEnd;
      if (isHorizontal) {
        paddingPercentageStart = padding.top / containerHeight;
        paddingPercentageEnd = padding.bottom / containerHeight;
      }
      else {
        paddingPercentageStart = padding.left / containerWidth;
        paddingPercentageEnd = padding.right / containerWidth;
      }

      var secondaryAxisRemapMultiplier = 1f - paddingPercentageStart - paddingPercentageEnd;
        
      var totalOffsetUntilThisRow = isHorizontal ? padding.left : padding.top;
      var currentRowSizeInScrollableAxis = 0f;
      var currentSizeInSecondaryAxisPerc = paddingPercentageStart;

      var direction = renderLatestItemsFirst ? -1 : 1;
      var iterationResult = ForEachElementActionResult.ContinueIterating;

      bool shouldContinueIterating() => iterationResult switch {
        ForEachElementActionResult.StopIterating => false,
        ForEachElementActionResult.ContinueIterating => true,
        _ => throw ExhaustiveMatch.Failed(iterationResult)
      };

      for (
        var idx = renderLatestItemsFirst ? iElementDatas.Count - 1 : 0;
        shouldContinueIterating() && renderLatestItemsFirst ? idx >= 0 : idx < iElementDatas.Count;
        idx += direction
      ) {
        var data = iElementDatas[idx];
        var itemSizeInSecondaryAxisPerc = data.sizeInSecondaryAxis.value * secondaryAxisRemapMultiplier;
        float itemLeftPerc;
        var rowSizeInScrollableAxis = data.sizeInScrollableAxis(isHorizontal: isHorizontal);
        if (currentSizeInSecondaryAxisPerc + itemSizeInSecondaryAxisPerc + paddingPercentageEnd > 1f + EPS) {
          itemLeftPerc = paddingPercentageStart;
          currentSizeInSecondaryAxisPerc = paddingPercentageStart + itemSizeInSecondaryAxisPerc;
          totalOffsetUntilThisRow += currentRowSizeInScrollableAxis + spacing;
          currentRowSizeInScrollableAxis = rowSizeInScrollableAxis;
        }
        else {
          itemLeftPerc = currentSizeInSecondaryAxisPerc;
          currentSizeInSecondaryAxisPerc += itemSizeInSecondaryAxisPerc;
          currentRowSizeInScrollableAxis = Mathf.Max(currentRowSizeInScrollableAxis, rowSizeInScrollableAxis);
        }

        Rect cellRect;
        if (isHorizontal) {
          var yPos = itemLeftPerc * containerHeight;
          var itemHeight = containerHeight * itemSizeInSecondaryAxisPerc;
            
          // NOTE: y axis goes up, but we want to place the items from top to bottom
          // Y = 0                  ------------------------------
          //                        |                            |
          // Y = -yPos              | ---------                  |
          //                        | |       |                  |
          //                        | | item  |     Container    |
          //                        | |       |                  |
          // Y = -yPos - itemHeight | ---------                  |
          //                        |                            |
          // Y = -containerHeight   |-----------------------------
            
          // cellRect pivot point (x,y) is at bottom left of the item
          cellRect = new Rect(
            x: totalOffsetUntilThisRow,
            y: -yPos - itemHeight,
            width: rowSizeInScrollableAxis,
            height: itemHeight
          );
        }
        else {
          var x = itemLeftPerc * containerWidth;
          cellRect = new Rect(
            x: x,
            y: -totalOffsetUntilThisRow - rowSizeInScrollableAxis,
            width: containerWidth * itemSizeInSecondaryAxisPerc,
            height: rowSizeInScrollableAxis
          );            
        }
             
        var placementVisible = visibleRect.Overlaps(cellRect, true);

        iterationResult = forEachElementAction(data, placementVisible, cellRect, dataA);
      }

      if (shouldContinueIterating()) {
        totalOffsetUntilThisRow += isHorizontal ? padding.right : padding.bottom;
        var containerSizeInScrollableAxis = totalOffsetUntilThisRow + currentRowSizeInScrollableAxis;
        return Some.a(new ForEachElementResult(containerSizeInScrollableAxis: containerSizeInScrollableAxis));
      }
      else {
        return None._;
      }
    }

    /// <inheritdoc cref="forEachElementStoppable{TElementData,Data}"/>
    public static ForEachElementResult forEachElement<TElementData, Data>(
      float spacing, IReadOnlyList<TElementData> iElementDatas,
      bool renderLatestItemsFirst, Padding padding, bool isHorizontal,
      RectTransform containersRectTransform, Rect visibleRect, Data dataA,
      ForEachElementAction<TElementData, Data> forEachElementAction
    ) where TElementData : IElement =>
      forEachElementStoppable(
        spacing: spacing, iElementDatas, renderLatestItemsFirst: renderLatestItemsFirst, padding,
        isHorizontal: isHorizontal, containersRectTransform: containersRectTransform, visibleRect: visibleRect,
        dataA: (forEachElementAction, dataA),
        forEachElementAction: static (elementData, visible, rect, tuple) => {
          tuple.forEachElementAction(elementData, visible, rect, tuple.dataA);
          return ForEachElementActionResult.ContinueIterating;
        }
      ).getOrThrow("this should be impossible");

    /// <summary>
    /// Calculates visible part of <see cref="container"/> using <see cref="maskRect"/> as viewport.
    /// </summary>
    public static Rect calculateVisibleRectStatic(RectTransform container, RectTransform maskRect) => 
      maskRect.rect.convertCoordinateSystem(((Transform) maskRect).some(), container);
      
    /// <summary>
    /// Is called when an <see cref="IElementDatas{TView}"/> becomes visible inside <see cref="_maskRect"/>.
    /// </summary>
    public static void updateVisibleElement<CommonDataType>(
      CommonDataType instance, RectTransform rt, Rect cellRect, Padding padding, Rect containerSize,
      ExpandElementsRectSizeInSecondaryAxis expandElements, bool isHorizontal
    ) where CommonDataType : IElement {
      if (expandElements == ExpandElementsRectSizeInSecondaryAxis.Expand) {
        if (isHorizontal) {
          rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            (containerSize.height - padding.vertical) * instance.sizeInSecondaryAxis.value
          );
        } 
        else {
          rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            (containerSize.width - padding.horizontal) * instance.sizeInSecondaryAxis.value
          );         
        }
      }
        
      // Call this first, because in there could be code which resizes this item's rectTransform.
      instance.onUpdateLayout(containerSize: containerSize, padding);

      rt.anchorMin = rt.anchorMax = Vector2.up;
      rt.localPosition = Vector3.zero;
      rt.anchoredPosition = cellRect.center;

#if UNITY_EDITOR
      if (!rt.pivot.approximately(new Vector2(0.5f, 0.5f))) {
        Log.d.error(
          $"This {nameof(DynamicLayout)} element has wrong pivot setup! This element will be positioned incorrectly! "
          + $"Needed: ({0.5f}, {0.5f}), Actual: {rt.pivot}", rt
        );
      }
#endif
    }

    /// <summary> What should happen after <see cref="_maskRect"/> gets resized. </summary>
    public static void onRectSizeChange(
      RectTransform container, ExpandElementsRectSizeInSecondaryAxis expandElements, bool isHorizontal, 
      float containerSizeInScrollableAxis, Rect rectSize
    ) {
      var rectTransformAxis = isHorizontal
        ? RectTransform.Axis.Horizontal
        : RectTransform.Axis.Vertical;
      container.SetSizeWithCurrentAnchors(rectTransformAxis, containerSizeInScrollableAxis);
    }
  }
}