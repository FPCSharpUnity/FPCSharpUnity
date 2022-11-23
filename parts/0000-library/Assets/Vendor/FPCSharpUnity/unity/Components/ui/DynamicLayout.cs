using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.data;
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
using FPCSharpUnity.core.typeclasses;
using FPCSharpUnity.unity.Pools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
  public partial class DynamicLayout : UIBehaviour, DynamicLayout.IDynamicLayout {
    #region Unity Serialized Fields
#pragma warning disable 649
// ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField, NotNull, PublicAccessor] ScrollRect _scrollRect;
    [SerializeField, NotNull, PublicAccessor] RectTransform _container;
    [SerializeField, NotNull, PublicAccessor] RectTransform _maskRect;
    [SerializeField, NotNull, PublicAccessor] Padding _padding;
    [SerializeField, FormerlySerializedAs("_spacing"), PublicAccessor] float _spacingInScrollableAxis;
    [SerializeField, InfoBox(
      DynamicLayout_ExpandElementsRectSizeInSecondaryAxisExts.SUMMARY_EXPAND_ELEMENTS_RECT_SIZE_IN_SECONDARY_AXIS
    ), PublicAccessor] ExpandElementsRectSizeInSecondaryAxis _expandElements;
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649
    #endregion

    public bool isHorizontal => scrollRect.horizontal;

    [DelegateToInterface(delegatedInterface = typeof(IDynamicLayout), delegateTo = nameof(backing))]
    public partial class Init<CommonDataType> : IModifyElementsList<CommonDataType>, IElements<CommonDataType>
      where CommonDataType : IElement 
    {
      public readonly IDynamicLayout backing;

      public IList<CommonDataType> items => _items;
      readonly List<CommonDataType> _items = new();
      
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
      void onEnable(GameObject gameObject) => ASync.NextFrame(gameObject, updateLayout);

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
          .toRxVal(() => mask.rect);

        if (isHorizontal && container.pivot != Vector2.up) {
          Debug.LogError($"Horizontal layout's content should have (0, 1) as pivot, not {container.pivot}!");
        }

        maskSize.zipSubscribe(containerSizeInScrollableAxis, tracker, (rectSize, size) => {
          Init.onRectSizeChange(container: container,
            expandElements: expandElements,
            isHorizontal: isHorizontal, containerSizeInScrollableAxis: size, rectSize: rectSize
          );
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
        _items.AddRange(elements);
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
      }
      
      public Option<Percentage> findItemsNormalizedScrollPositionForItem(Func<CommonDataType, bool> predicate) {
        var result = Option<Rect>.None;
        var forEachResult = forEachElement(
          predicate, (data, isVisible, cellRect, predicate_) => {
            if (predicate_(data)) {
              result = Some.a(cellRect);
            }
          }
        );
        {if (result.valueOut(out var cellRect)) {
          var viewportSize = maskRect.rect;
          var scrollPosition = isHorizontal
            ? (cellRect.center.x - viewportSize.width / 2f) / (forEachResult.containerSizeInScrollableAxis - viewportSize.width)
            : 1f - (Mathf.Abs(cellRect.center.y) - viewportSize.height / 2f) / (forEachResult.containerSizeInScrollableAxis - maskRect.rect.height);
          
          return Some.a(new Percentage(scrollPosition));
        } else {
          return None._;
        }}
      }

      public delegate Option<A> FindItemPredicate<A>(CommonDataType data, Rect cellRect);

      public RectTransform elementsParent => (RectTransform)container.transform;
      public CommonDataType getItemAt(int idx) => items[idx];
      public void removeItemAt(int idx) => items.RemoveAt(idx);
      public int itemsCount => items.Count;

      public Option<B> findItem<B>(FindItemPredicate<B> predicate) {
        var result = Option<B>.None;
        forEachElementStoppable(
          predicate, 
          (data, isVisible, cellRect, predicate_) => {
            if (predicate_(data, cellRect).valueOut(out var match)) {
              result = Some.a(match);
              return ForEachElementActionResult.StopIterating;
            }
            else {
              return ForEachElementActionResult.ContinueIterating;
            }
          }
        );
        return result;
      }

      public Option<B> findItem<B>(Func<CommonDataType, Rect, Option<B>> predicate) {
        throw new NotImplementedException();
      }

      public ImmutableArrayC<A> collectItems<A>(Func<CommonDataType, Rect, Option<A>> predicate) {
        var result = new ImmutableArrayCBuilder<A>();
        forEachElement(
          predicate, 
          (data, isVisible, cellRect, predicate_) => {
            if (predicate_(data, cellRect).valueOut(out var match)) {
              result.add(match);
            }
          } 
        );
        return result.build();
      }

      /// <inheritdoc cref="Init.forEachElement{TElementData,Data}"/>
      ForEachElementResult forEachElement<Data>(
        Data dataA, ForEachElementAction<CommonDataType, Data> updateElement
      ) =>
        Init.forEachElement(
          spacing: spacingInScrollableAxis, iElementDatas: _items,
          renderLatestItemsFirst: renderLatestItemsFirst, padding: padding, isHorizontal: isHorizontal,
          containersRectTransform: container, visibleRect: calculateVisibleRect, dataA: dataA,
          forEachElementAction: updateElement
        );

      /// <inheritdoc cref="Init.forEachElementStoppable{TElementData,Data}"/>
      Option<ForEachElementResult> forEachElementStoppable<Data>(
        Data dataA, ForEachElementActionStoppable<CommonDataType, Data> updateElement
      ) =>
        Init.forEachElementStoppable(
          spacing: spacingInScrollableAxis, iElementDatas: _items,
          renderLatestItemsFirst: renderLatestItemsFirst, padding: padding, isHorizontal: isHorizontal,
          containersRectTransform: container, visibleRect: calculateVisibleRect, dataA: dataA,
          forEachElementAction: updateElement
        );
    }
  }
}