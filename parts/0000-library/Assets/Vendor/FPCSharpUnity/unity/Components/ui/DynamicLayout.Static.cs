using System;
using System.Collections.Generic;
using System.Linq;
using ExhaustiveMatching;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.pools;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.Utilities;
using JetBrains.Annotations;
using Smooth.Collections;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui {
  public static partial class DynamicLayoutExts {
  
    /// <summary> Item id type for identifying items between data updates. </summary>
    // Used for XML docs only.
    [PublicAPI] static void idTypeDescription(){}
  
    /// <summary>
    /// Implement this in your <see cref="DynamicLayout.IElement"/> items if you want for them to be updatable. It's used
    /// for <see cref="DynamicLayoutExts.replaceAllElementsData{CommonDataType,TData,TId}"/>.
    /// </summary>
    /// <typeparam name="InnerData">Data that we want to update inside <see cref="DynamicLayout.IElement"/>.</typeparam>
    /// <typeparam name="IdType">See <see cref="DynamicLayoutExts.idTypeDescription"/>.</typeparam>
    public interface ILayoutElementUpdatable<InnerData, out IdType> : DynamicLayout.IElement
      where IdType : IEquatable<IdType> 
    {
      /// <inheritdoc cref="DynamicLayoutExts.idTypeDescription"/>
      IdType getId { get; }
    
      /// <summary>
      /// Example: the <see cref="DynamicLayout"/> we want to use supports two <see cref="DynamicLayout.IElement"/> types:
      /// RowElement and SpacerElement. RowElement has specific data (<see cref="InnerData"/>) inside that we want to
      /// update without clearing whole layout and re-adding every element back. While SpacerElement doesn't need any data
      /// to be updated and so we return `None` here. This means that only a subset of supported
      /// <see cref="DynamicLayout.IElement"/>s that have <see cref="InnerData"/> inside needs be updated.
      /// <para/>
      /// If this is `None`, we will not call <see cref="updateData"/> on the item.
      /// </summary>
      Option<InnerData> extractDataForUpdating { get; }
    
      /// <summary>
      /// Update element with new data.
      /// <para/>
      /// We can't just remove old element and replace it by new one instead of doing this 'data-swap'. <br/>
      /// That's because hiding and then showing item's visual causes Unity object's and it's components' state to be
      /// reset (mainly hover state).
      /// </summary>
      void updateData(InnerData newData);
    }

    /// <summary>
    /// Update items without resetting their visuals that are already visible by the user. It is useful when the data
    /// constantly changes and user wants to interact with items without them changing their order inside scrollView. Use
    /// this updating instead of full update approach (clear all, and re-add all) if the order of items are not important.
    /// </summary>
    /// <param name="layout">Dynamic layout we want to update.</param>
    /// <param name="newDatas">Updated list of layout elements we want to show in dynamic layout.</param>
    /// <param name="maybeComparableForSorting">
    /// If provided, the layout elements will be sorted using this comparable.
    /// </param>
    /// <typeparam name="CommonDataType">
    /// <see cref="DynamicLayout.IElement"/> type that is supported by <see cref="layout"/> elements.
    /// </typeparam>
    /// <typeparam name="TData">A subset of data types we want to update inside <see cref="CommonDataType"/>.</typeparam>
    /// <typeparam name="TId">See <see cref="idTypeDescription"/>.</typeparam>
    public static void replaceAllElementsData<CommonDataType, TData, TId>(
      this DynamicLayout.IModifyElementsList<CommonDataType> layout, IReadOnlyList<CommonDataType> newDatas,
      Option<Comparable<CommonDataType>> maybeComparableForSorting = default
    )
      where TId : IEquatable<TId> 
      where CommonDataType : ILayoutElementUpdatable<TData, TId> 
    {
      using var newDatasDictDisposable = DictionaryPool<TId, CommonDataType>.instance.BorrowDisposable();
      var newDatasDict = newDatasDictDisposable.value;

      foreach (var data in newDatas) {
        newDatasDict[data.getId] = data;
      }

      // Update existing items with new data and remove old items that are not present in newDatas.
      for (var i = 0; i < layout.items.Count; i++) {
        var item = layout.items[i];
        if (newDatasDict.TryGetValue(item.getId, out var tpl)) {
          // Update with provided data if it is supported by the item.
          if (tpl.extractDataForUpdating.valueOut(out var newData)) {
            item.updateData(newData);
            if (item.isVisible) item.showOrUpdate(parent: layout.elementsParent, forceUpdate: true);            
          }
          newDatasDict.Remove(item.getId);
        }
        else {
          item.hide();
          layout.items.RemoveAt(i);
          i--;
        } 
      }
    
      // Iterate through initial list first. This way we ensure that the item's order was not modified by dictionary
      // ordering.
      foreach (var element in newDatas) {
        // Add items that were not present before this update.
        if (newDatasDict.ContainsKey(element.getId)) {
          layout.appendDataIntoLayoutData(element, updateLayout: false);
        }
      }

      {if (maybeComparableForSorting.valueOut(out var comparable)) {
        layout.items.stableSort(comparable);
      }}
    
      layout.updateLayout();
    }
    
    /// <summary>
    /// Updates <see cref="DynamicLayout"/> layout elements' list using provided <see cref="newItems"/> list. It will
    /// try and match already present layout items inside <see cref="DynamicLayout"/> by matching same
    /// <see cref="CommonInnerData"/>s together. If the new item is not present, it will be added by constructing it
    /// using <see cref="toLayoutElement"/> callback. If item is present, it will not be updated, just placed into
    /// correct position on screen. If item that was in layout list is not in <see cref="newItems"/>, it will be hidden
    /// and disposed of.
    /// <para/>
    /// Use this if all <see cref="layout"/> elements' <see cref="CommonInnerData"/> can be expressed by the same one
    /// type and each of <see cref="DynamicLayout.ElementWithInnerData{A}.data"/> can be distinguished from one another.
    /// Example: use union type for <see cref="CommonInnerData"/>, which consists of all
    /// <see cref="CommonDynamicElementType"/> inner datas.
    /// <para/>
    /// Use this method instead of using <see cref="DynamicLayout.Init{A}.clearLayoutData"/>+
    /// <see cref="DynamicLayout.Init{A}.appendDataIntoLayoutData(A, bool)"/>+
    /// <see cref="DynamicLayout.Init{A}.updateLayout"/> combo, as it puts all visible UI items back to pool and
    /// takes them out again, and it is performance intensive to toggle UI components in Unity.
    /// </summary>
    /// <param name="layout">Layout to update.</param>
    /// <param name="newItems">New list of items to put inside</param>
    /// <param name="data">Extra type to prevent closures.</param>
    /// <param name="toLayoutElement">Construct dynamic layout element from given data.</param>
    /// <typeparam name="CommonDynamicElementType">A type for all `<see cref="layout"/>` items.</typeparam>
    /// <typeparam name="CommonInnerData">
    /// Combined union type which has inner datas for all `<see cref="CommonDynamicElementType"/>` types.
    /// </typeparam>
    /// <typeparam name="Data">Extra type to prevent closures.</typeparam>
    public static void updateLayoutWithCommonInnerType<CommonDynamicElementType, CommonInnerData, Data, Key>(
      this DynamicLayout.IModifyElementsList<CommonDynamicElementType> layout,
      IReadOnlyList<CommonInnerData> newItems, Data data, 
      Func<CommonInnerData, Data, CommonDynamicElementType> toLayoutElement,
      Func<CommonInnerData, Key> getKey
    )
      where CommonDynamicElementType 
      : DynamicLayout.ElementWithInnerDataSettable<CommonInnerData>, 
        IEquatable<CommonDynamicElementType>, 
        DynamicLayout.IElement
      where CommonInnerData : IEquatable<CommonInnerData> 
      where Key : IEquatable<Key> 
    {
      using var previousItemsSet = 
        DictionaryPool<Key, Pool<List<CommonDynamicElementType>>.Disposable>.instance.BorrowDisposable();

      foreach (var item in layout.items) {
        addVisualToSet(item.data, item);
      }
      layout.items.Clear();

      // ReSharper disable once ForCanBeConvertedToForeach
      // foreach loop would allocate here.
      for (var idx = 0; idx < newItems.Count; idx++) {
        var newInnerData = newItems[idx];
        layout.items.Add(
          removeVisualFromSetIfExists(newInnerData).valueOut(out var existingItem)
            ? existingItem
            : toLayoutElement(newInnerData, data)
        );
      }
      
      disposeRemainingElements();
      layout.updateLayout();

      void addVisualToSet(CommonInnerData innerData, CommonDynamicElementType value) {
        var key = getKey(innerData);
        var list = previousItemsSet.value.getOrUpdate(
          key, static () => ListPool<CommonDynamicElementType>.instance.BorrowDisposable()
        );
        list.value.Add(value);
      }
      
      Option<CommonDynamicElementType> removeVisualFromSetIfExists(CommonInnerData innerData) {
        var key = getKey(innerData);
        var list = previousItemsSet.value.get(key).getOr_RETURN_NONE();
        var value = list.value.last().getOr_RETURN_NONE();
        list.value.RemoveLast();
        if (list.value.isEmpty()) {
          previousItemsSet.value.Remove(key);
          list.Dispose();
        }

        if (!innerData.Equals(value.data)) {
          value.dataSetter = innerData;
          value.updateStateIfVisible();
        }
        return Some.a(value);
      }
      
      void disposeRemainingElements() {
        foreach (var kvp in previousItemsSet.value) {
          foreach (var element in kvp.Value.value) {
            element.hide();
          }
          kvp.Value.Dispose();
        }
      }
    }
  }

  public partial class DynamicLayout {
    public static class Init {
      const float EPS = 1e-9f;

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
}