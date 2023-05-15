using System;
using System.Collections.Generic;
using System.Linq;
using ExhaustiveMatching;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.pools;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.Utilities;
using JetBrains.Annotations;
using Smooth.Collections;
using UnityEngine;
using AnyExts = FPCSharpUnity.core.exts.AnyExts;

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
        float scrAxisSpacing, int itemsCount, Func<int, Data, TElementData> getElement, 
        bool renderLatestItemsFirst, Padding padding, bool isHorizontal, 
        RectTransform containersRectTransform, Rect visibleRect, Data dataA, 
        ForEachElementActionStoppable<TElementData, Data> forEachElementAction,
        float secAxisSpacing, Func<TElementData, SizeProvider> extractSizeProvider
      ) {
        var containerRect = containersRectTransform.rect;

        // Depending on orientation it's top or left
        float secAxisPaddingStart;
        // Depending on orientation it's bottom or right
        float secAxisPaddingEnd;
        
        float secAxisMaxSizeMinusPadding;
        float secAxisMaxSize;
        float scrAxisCurrentOffset;
        if (isHorizontal) {
          secAxisPaddingStart = padding.top;
          secAxisPaddingEnd = padding.bottom;
          secAxisMaxSize = containerRect.height;
          secAxisMaxSizeMinusPadding = containerRect.height - padding.vertical;
          scrAxisCurrentOffset = padding.left;
        }
        else {
          secAxisPaddingStart = padding.left;
          secAxisPaddingEnd = padding.right;
          secAxisMaxSize = containerRect.width;
          secAxisMaxSizeMinusPadding = containerRect.width - padding.horizontal;
          scrAxisCurrentOffset = padding.top;
        }
        
        var scrAxisCurrentRowSize = 0f;
        var secAxisCurrentOffset = secAxisPaddingStart;

        var direction = renderLatestItemsFirst ? -1 : 1;
        var iterationResult = ForEachElementActionResult.ContinueIterating;

        bool shouldContinueIterating() => iterationResult switch {
          ForEachElementActionResult.StopIterating => false,
          ForEachElementActionResult.ContinueIterating => true,
          _ => throw ExhaustiveMatch.Failed(iterationResult)
        };

        for (
          var idx = renderLatestItemsFirst ? itemsCount - 1 : 0;
          shouldContinueIterating() && (renderLatestItemsFirst ? idx >= 0 : idx < itemsCount);
          idx += direction
        ) {
          var data = getElement(idx, dataA);
          var sizeProvider = extractSizeProvider(data);
          var secAxisItemSize = 
            sizeProvider.itemSizeInSecondaryAxis.calculate(secAxisMaxSizeMinusPadding, isHorizontal);

          float secAxisOffsetBeforeThisItem;
          var scrAxisItemSize = sizeProvider.sizeInScrollableAxis.calculate(isHorizontal: isHorizontal);
          bool gotMovedToNextRow;
          
          {if (
            sizeProvider.spacingAfterItemSizeInSecondaryAxis
              .foldM(
                () => secAxisSpacing > 0 ? Some.a(secAxisSpacing) : None._,
                itemSpacing => Some.a(
                  secAxisSpacing + itemSpacing.calculate(secAxisMaxSizeMinusPadding, isHorizontal)
                )
              )
              .valueOut(out var secAxisItemSpaceAfter)
          ) {
            var secAxisItemSizePlusSpacing = secAxisItemSize + secAxisItemSpaceAfter;
            if (fitsInCurrentRow(secAxisItemSizePlusSpacing)) {
              updateForFittingInCurrentRow(secAxisItemSizePlusSpacing);
            }          
            else if (fitsInCurrentRow(secAxisItemSize)) {
              updateForFittingInCurrentRow(secAxisItemSize);
            }
            else {
              updateForNotFittingInCurrentRow(secAxisItemSizePlusSpacing);
            }             
          } else {
            if (fitsInCurrentRow(secAxisItemSize)) {
              updateForFittingInCurrentRow(secAxisItemSize);
            }
            else {
              updateForNotFittingInCurrentRow(secAxisItemSize);
            }            
          }}

          bool fitsInCurrentRow(float secAxisItemSize_) =>
            secAxisCurrentOffset + secAxisItemSize_ + secAxisPaddingEnd <= secAxisMaxSize + EPS;

          void updateForNotFittingInCurrentRow(float secAxisItemSize_) {
            secAxisOffsetBeforeThisItem = secAxisPaddingStart;
            secAxisCurrentOffset = secAxisPaddingStart + secAxisItemSize_;
            scrAxisCurrentOffset += scrAxisCurrentRowSize + scrAxisSpacing;
            scrAxisCurrentRowSize = scrAxisItemSize;
            gotMovedToNextRow = true;
          }

          void updateForFittingInCurrentRow(float secAxisItemSize_) {
            secAxisOffsetBeforeThisItem = secAxisCurrentOffset;
            secAxisCurrentOffset += secAxisItemSize_;
            scrAxisCurrentRowSize = Mathf.Max(scrAxisCurrentRowSize, scrAxisItemSize);
            gotMovedToNextRow = false;
          }

          Rect cellRect;
          if (isHorizontal) {
            var yPos = secAxisOffsetBeforeThisItem;
            var itemHeight = secAxisItemSize;
            
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
              x: scrAxisCurrentOffset,
              y: -yPos - itemHeight,
              width: scrAxisItemSize,
              height: itemHeight
            );
          }
          else {
            var x = secAxisOffsetBeforeThisItem;
            cellRect = new Rect(
              x: x,
              y: -scrAxisCurrentOffset - scrAxisItemSize,
              width: secAxisItemSize,
              height: scrAxisItemSize
            );            
          }
             
          var placementVisible = visibleRect.Overlaps(cellRect, true);

          iterationResult = forEachElementAction(data, placementVisible, cellRect, dataA, gotMovedToNextRow);
        }

        if (shouldContinueIterating()) {
          scrAxisCurrentOffset += isHorizontal ? padding.right : padding.bottom;
          var containerSizeInScrollableAxis = scrAxisCurrentOffset + scrAxisCurrentRowSize;
          return Some.a(new ForEachElementResult(containerSizeInScrollableAxis: containerSizeInScrollableAxis));
        }
        else {
          return None._;
        }
      }

      /// <inheritdoc cref="forEachElementStoppable{TElementData,Data}"/>
      public static ForEachElementResult forEachElement<TElementData, Data>(
        float scrAxisSpacing, IReadOnlyList<TElementData> iElementDatas,
        bool renderLatestItemsFirst, Padding padding, bool isHorizontal,
        RectTransform containersRectTransform, Rect visibleRect, Data dataA,
        ForEachElementAction<TElementData, Data> forEachElementAction,
        float secAxisSpacing
      ) where TElementData : IHasSizeProvider =>
        forEachElementStoppable(
          scrAxisSpacing: scrAxisSpacing, itemsCount: iElementDatas.Count, 
          getElement: static (i, t) => t.iElementDatas[i],
          renderLatestItemsFirst: renderLatestItemsFirst, padding,
          isHorizontal: isHorizontal, containersRectTransform: containersRectTransform, visibleRect: visibleRect,
          dataA: (forEachElementAction, dataA, iElementDatas),
          forEachElementAction: static (elementData, visible, rect, tuple, _) => {
            tuple.forEachElementAction(elementData, visible, rect, tuple.dataA);
            return ForEachElementActionResult.ContinueIterating;
          },
          secAxisSpacing: secAxisSpacing, extractSizeProvider: static e => e.sizeProvider
        ).getOrThrow("this should be impossible");

      /// <summary>
      /// Calculates visible part of <see cref="container"/> using <see cref="maskRect"/> as viewport.
      /// </summary>
      public static Rect calculateVisibleRectStatic(RectTransform container, RectTransform maskRect) => 
        maskRect.rect.convertCoordinateSystem(Some.a(((Transform) maskRect)), container);
      
    
    
      /// <summary>
      /// Is called when an <see cref="IElementDatas{TView}"/> becomes visible inside <see cref="_maskRect"/>.
      /// </summary>
      public static void updateVisibleElement<CommonDataType>(
        CommonDataType instance, RectTransform rt, Rect cellRect, Padding padding, Rect containerSize,
        ExpandElementsRectSizeInSecondaryAxis expandElements, bool isHorizontal
      ) where CommonDataType : IElement {
        if (expandElements == ExpandElementsRectSizeInSecondaryAxis.Expand) {
          if (isHorizontal) {
            rt.SetSizeWithCurrentAnchors(
              RectTransform.Axis.Vertical,
              instance.sizeProvider.itemSizeInSecondaryAxis.calculate(
                containerSize.height - padding.vertical, isHorizontal: true
              )
            );
          } 
          else {
            rt.SetSizeWithCurrentAnchors(
              RectTransform.Axis.Horizontal,
              instance.sizeProvider.itemSizeInSecondaryAxis.calculate(
                containerSize.width - padding.horizontal, isHorizontal: false
              )
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