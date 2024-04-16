using System;
using System.Collections.Generic;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.unity.Components.Forwarders;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Reactive;
using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.pools;
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Logger;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  /// <summary>
  /// Scrollable vertical/horizontal layout, which makes sure that only visible elements are created.
  /// Element is considered visible if it intersects with <see cref="maskRect"/> bounds.
  /// <para/>
  /// Sample vertical layout:
  /// 
  /// <code><![CDATA[
  ///  #  | height | width
  ///  0    10       33%
  ///  1    30       33%
  ///  2    10       33%
  ///  3    10       50%
  ///  4    10       100%
  ///
  /// +-----+-----+-----+
  /// |  0  |  1  |  2  |
  /// +-----|     |-----+
  ///       |     |
  /// +-----+--+--+
  /// |    3   |
  /// +--------+--------+
  /// |        4        |
  /// +-----------------+
  /// ]]></code>
  /// </summary>
  [HasLogger] public partial class DynamicLayout : UIBehaviour, DynamicLayout.IDynamicLayout {
    #region Unity Serialized Fields
#pragma warning disable 649
// ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField, NotNull, PublicAccessor] ScrollRect _scrollRect;
    [SerializeField, NotNull, PublicAccessor] RectTransform _container;
    [SerializeField, NotNull, PublicAccessor] RectTransform _maskRect;
    [SerializeField, NotNull] Padding _padding;
    [SerializeField, FormerlySerializedAs("_spacing"), PublicAccessor] float _spacingInScrollableAxis;
    [
      SerializeField, PublicAccessor, InfoBoxButton("Spacing between each item in non scrollable axis.")
    ] float _spacingInSecondaryAxis;
    [SerializeField, InfoBox(
      DynamicLayout_ExpandElementsRectSizeInSecondaryAxisExts.SUMMARY_EXPAND_ELEMENTS_RECT_SIZE_IN_SECONDARY_AXIS
    ), PublicAccessor] ExpandElementsRectSizeInSecondaryAxis _expandElements;
    // Default as true did not work for some reason.
    [SerializeField, InfoBox("When False, Resets scroll position OnEnable")] bool _onEnableDoNotResetPosition;
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649
    #endregion
    
    public Padding padding { get => _padding; set => _padding = value; }
    
    public bool onEnableResetPosition => !_onEnableDoNotResetPosition;

    public bool isHorizontal => scrollRect.horizontal;

    partial void _editor_layoutUpdated<CommonDataType>(IReadOnlyList<CommonDataType> list) where CommonDataType : IElement;

    public class Init<CommonDataType> : IModifyElementsList<CommonDataType>, IElements<CommonDataType>, IDynamicLayout
      where CommonDataType : IElement 
    {
      public readonly IDynamicLayout backing;
      public List<CommonDataType> items { get; } = new();

      public bool isHorizontal => backing.isHorizontal;
      public ScrollRect scrollRect => backing.scrollRect;
      public RectTransform container => backing.container;
      public RectTransform maskRect => backing.maskRect;
      public bool onEnableResetPosition => backing.onEnableResetPosition;

      public Padding padding {
        get => backing.padding;
        set {
          backing.padding = value;
          updateLayout();
        }
      }

      public float spacingInScrollableAxis => backing.spacingInScrollableAxis;
      public float spacingInSecondaryAxis => backing.spacingInSecondaryAxis;
      public ExpandElementsRectSizeInSecondaryAxis expandElements => backing.expandElements;

      /// <summary> How much space all layout elements takes up in scrollable axis. </summary>
      readonly IRxRef<float> containerSizeInScrollableAxis = RxRef.a(0f);
      
      /// <summary>
      /// If true - elements in UI are ordered in reversed order from <see cref="items"/>.
      /// If false - elements in UI are ordered in same order as <see cref="items"/>.<br/>
      /// </summary>
      readonly bool renderLatestItemsFirst;
      
      /// <summary> A reactive value of <see cref="maskRect"/> size. </summary>
      public readonly IRxVal<Rect> maskSize;
      
      // When we add elements to layout and enable it on the same frame,
      // layout does not work correctly due to rect sizes == 0.
      // Unable to solve this properly. NextFrame is a workaround. 
      void onEnable(GameObject gameObject) {
        if (backing.onEnableResetPosition) {
          resetScrollPosition();
        }
        ASync.NextFrame(gameObject, updateLayout);
      }

      public void resetScrollPosition() {
        var rect = backing.scrollRect;
        if (rect.vertical) {
          rect.verticalNormalizedPosition = 0;
        }
        if (rect.horizontal) {
          rect.horizontalNormalizedPosition = 0;
        }
      }

      public Init(
        DynamicLayout backing,
        ITracker dt,
        bool renderLatestItemsFirst = false
      ) : this(
        layout: backing, dt, renderLatestItemsFirst
      ) {
        backing._scrollRect.onValueChanged.subscribe(dt, _ => updateLayout());
      }

      /// <summary>
      /// Overload for uses when <see cref="DynamicLayout"/> logic is needed without scrollable container.
      /// </summary>
      public Init(
        IDynamicLayout layout,
        ITracker tracker,
        bool renderLatestItemsFirst = false
      ) {
        this.renderLatestItemsFirst = renderLatestItemsFirst;
        backing = layout;
        
#if UNITY_EDITOR
        backing.downcast(default(DynamicLayout)).ifSomeM(l => {
          tracker.track(() => l._editor_layoutUpdated(EmptyArray<CommonDataType>._));
        });
#endif

        // When we add elements to layout and enable it on the same frame,
        // layout does not work correctly due to rect sizes == 0.
        // Unable to solve this properly. NextFrame is a workaround.
        container.gameObject.EnsureComponent<OnEnableForwarder>().onEvent.subscribe(tracker,
          _ => onEnable(container.gameObject)
        );
        tracker.track(clearLayoutData);

        // We need oncePerFrame() because Unity doesn't allow doing operations like gameObject.SetActive()
        // from OnRectTransformDimensionsChange()
        // oncePerFrame() performs operation in LateUpdate
        // ReSharper disable once LocalVariableHidesMember
        var maskSize = this.maskSize = maskRect.gameObject.EnsureComponent<OnRectTransformDimensionsChangeForwarder>()
          .rectDimensionsChanged
          .oncePerFrame()
          .filter(_ => maskRect) // maskRect can go away before late update, so double check it.
          .toRxVal(() => maskRect.rect);

        if (isHorizontal && container.pivot != Vector2.up) {
          log.error($"Horizontal layout's content should have (0, 1) as pivot, not {container.pivot}!");
        }

        maskSize.zipSubscribe(containerSizeInScrollableAxis, tracker, (rectSize, size) => {
          Init.onRectSizeChange(container: container,
            expandElements: expandElements,
            isHorizontal: isHorizontal, containerSizeInScrollableAxis: size, rectSize: rectSize
          );
          // log.error(
          //   $"{scrollRect.transform.parent.name}: {rectSize.echo()}, {size.echo()}, {container.sizeDelta.echo()}",
          //   scrollRect.gameObject
          // ); 
          updateLayout();
        });
      }

      /// <param name="element"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      [PublicAPI]
      public void appendDataIntoLayoutData(CommonDataType element, bool updateLayout = true) {       
        items.Add(element);
        if (updateLayout) this.updateLayout();
      }

      /// <param name="elements"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      [PublicAPI]
      public void appendDataIntoLayoutData(IEnumerable<CommonDataType> elements, bool updateLayout = true) {
        items.AddRange(elements);
        if (updateLayout) this.updateLayout();
      }

      [PublicAPI]
      public void clearLayoutData() {
        foreach (var item in items) {
          item.hide();
        }
        items.Clear();
      }

      public Rect calculateVisibleRect => Init.calculateVisibleRectStatic(container: container, maskRect: maskRect);
      
      [PublicAPI] public void updateLayout() {
        var result = forEachElement(
          this, static (data, placementVisible, cellRect, self) => {
            switch (placementVisible) {
              case true: {
                if (data.showOrUpdate(self.container, forceUpdate: false).valueOut(out var rt)) {
                  Init.updateVisibleElement(
                    data, rt, cellRect: cellRect, padding: self.padding, isHorizontal: self.isHorizontal,
                    expandElements: self.expandElements, containerSize: self.container.rect
                  );
                }
                break;
              }
              case false when data.isVisible: {
                data.hide();
                break;
              }
            }
          }
        );
        
        containerSizeInScrollableAxis.value = result.containerSizeInScrollableAxis;
#if UNITY_EDITOR
        backing.downcast(default(DynamicLayout)).ifSomeM(l => l._editor_layoutUpdated(items));
#endif
      }
      
      public Option<Percentage> findItemsNormalizedScrollPositionForItem(Func<CommonDataType, bool> predicate) {
        using var _ = RefOptionPool<Rect>.instance.borrowDisposable(out var resultRef);
        var forEachResult = forEachElement(
          (predicate, resultRef), 
          static (data, isVisible, cellRect, tpl) => {
            if (tpl.predicate(data)) {
              tpl.resultRef.value = Some.a(cellRect);
            }
          }
        );
        {if (resultRef.value.valueOut(out var cellRect)) {
          var viewportSize = maskRect.rect;
          var scrollPosition = isHorizontal
            ? (cellRect.center.x - viewportSize.width / 2f) / (forEachResult.containerSizeInScrollableAxis - viewportSize.width)
            : 1f - (Mathf.Abs(cellRect.center.y) - viewportSize.height / 2f) / (forEachResult.containerSizeInScrollableAxis - maskRect.rect.height);
          
          return Some.a(new Percentage(scrollPosition));
        } else {
          return None._;
        }}
      }

      /// <summary> A filter to find specific layout element data. </summary>
      public delegate Option<A> FindItemPredicate<A>(CommonDataType data, Rect cellRect);

      public RectTransform elementsParent => (RectTransform)container.transform;
      public CommonDataType getItemAt(int idx) => items[idx];
      public void removeItemAt(int idx) => items.RemoveAt(idx);
      public int itemsCount => items.Count;

      public Option<B> findItem<B>(FindItemPredicate<B> predicate) =>
        forEachElementStoppable(
          predicate, 
          static (data, isVisible, cellRect, predicate_, _) => predicate_(data, cellRect)
        ).leftValue;

      public ImmutableArrayC<A> collectItems<A>(Func<CommonDataType, Rect, Option<A>> predicate) {
        var builder = new ImmutableArrayCBuilder<A>();
        forEachElement(
          (predicate, builder), 
          static (data, isVisible, cellRect, tpl) => {
            if (tpl.predicate(data, cellRect).valueOut(out var match)) {
              tpl.builder.add(match);
            }
          } 
        );
        return builder.build();
      }

      /// <inheritdoc cref="Init.forEachElement{TElementData,Data}"/>
      ForEachElementResult forEachElement<Data>(
        Data dataA, ForEachElementAction<CommonDataType, Data> updateElement
      ) =>
        Init.forEachElement(
          scrAxisSpacing: spacingInScrollableAxis, iElementDatas: items,
          renderLatestItemsFirst: renderLatestItemsFirst, padding: padding, isHorizontal: isHorizontal,
          containersRectTransform: container, visibleRect: calculateVisibleRect, dataA: dataA,
          forEachElementAction: updateElement, secAxisSpacing: spacingInSecondaryAxis
        );

      /// <inheritdoc cref="Init.forEachElementStoppable{TElementData,Data}"/>
      Either<TStoppedEarly, ForEachElementResult> forEachElementStoppable<TStoppedEarly, Data>(
        Data dataA, ForEachElementActionStoppable<CommonDataType, TStoppedEarly, Data> updateElement
      ) =>
        Init.forEachElementStoppable(
          scrAxisSpacing: spacingInScrollableAxis, itemsCount: items.Count, getElement: static (i, t) => t.items[i],
          renderLatestItemsFirst: renderLatestItemsFirst, padding: padding, isHorizontal: isHorizontal,
          containersRectTransform: container, visibleRect: calculateVisibleRect, dataA: (dataA, items, updateElement),
          forEachElementAction: static (elementData, placementVisible, cellRect, t, gotMovedToNextRow) => 
            t.updateElement(elementData, placementVisible, cellRect, t.dataA, gotMovedToNextRow), 
          secAxisSpacing: spacingInSecondaryAxis,
          extractSizeProvider: static t => t.sizeProvider
        );
    }
  }
}