using System;
using System.Collections.Generic;
using System.Linq;
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
using FPCSharpUnity.core.typeclasses;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  /// <summary>
  /// Scrollable vertical/horizontal layout, which makes sure that only visible elements are created.
  /// Element is considered visible if it intersects with <see cref="_maskRect"/> bounds.
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
  public partial class DynamicLayout : UIBehaviour {
    #region Unity Serialized Fields

#pragma warning disable 649
// ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField, NotNull, PublicAccessor] ScrollRect _scrollRect;
    [SerializeField, NotNull] RectTransform _container;
    [SerializeField, NotNull, PublicAccessor] RectTransform _maskRect;
    [SerializeField, NotNull, PublicAccessor] Padding _padding;
    [SerializeField, FormerlySerializedAs("_spacing")] float _spacingInScrollableAxis;
    [SerializeField, InfoBox(
      DynamicLayout_ExpandElementsRectSizeInSecondaryAxisExts.SUMMARY_EXPAND_ELEMENTS_RECT_SIZE_IN_SECONDARY_AXIS
    )] ExpandElementsRectSizeInSecondaryAxis _expandElements;
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    #endregion

    public class Init<TData, TView> : ILayout, IElements<TData>, IVisibleElements<TData, TView>
      where TData : IElementData<TView> 
      where TView : IElementView 
    {
      /// <summary> A place where all layout elements gets put into. </summary>
      readonly RectTransform _container;
      
      /// <summary>
      /// A viewport where all layout elements are rendered in if they are inside this rect. Its name has a 'mask' in
      /// it, because in most of the cases, this <see cref="RectTransform"/> has <see cref="RectMask2D"/> component
      /// attached to it as well.
      /// </summary>
      readonly RectTransform _maskRect;
      
      /// <summary> All layout elements that are present in this layout. </summary>
      readonly List<TData> layoutData;
      
      /// <summary> How much space all layout elements takes up in scrollable axis. </summary>
      readonly IRxRef<float> containerSizeInScrollableAxis = RxRef.a(0f);
      
      /// <summary>
      /// If true - elements in UI are ordered in reversed order from <see cref="layoutData"/>.
      /// If false - elements in UI are ordered in same order as <see cref="layoutData"/>.<br/>
      /// </summary>
      readonly bool renderLatestItemsFirst;
      
      /// <summary>
      /// Whether <see cref="DynamicLayout._scrollRect"/> is horizontal or vertical. Can't be both.
      /// </summary>
      readonly bool isHorizontal;
      
      /// <summary>
      /// How many UI units to move all layout elements away from the <see cref="_container"/> sides.
      /// </summary>
      readonly Padding padding;
      
      /// <summary> A spacing between layout elements. </summary>
      readonly float spacingInScrollableAxis;
      
      /// <inheritdoc cref="ExpandElementsRectSizeInSecondaryAxis"/>
      readonly ExpandElementsRectSizeInSecondaryAxis expandElements;
      
      /// <summary> A reactive value of <see cref="_maskRect"/> size. </summary>
      public readonly IRxVal<Rect> maskSize;

      /// <summary>
      /// If <see cref="Option{A}"/> is `None`, that means there is no backing view, it is only a spacer.
      /// </summary>
      readonly Dictionary<TData, Option<TView>> _items = new();

      public Option<Option<TView>> get(TData key) => _items.get(key);
      
      // When we add elements to layout and enable it on the same frame,
      // layout does not work correctly due to rect sizes == 0.
      // Unable to solve this properly. NextFrame is a workaround. 
      void onEnable(GameObject gameObject) => ASync.NextFrame(gameObject, updateLayout);

      public Init(
        DynamicLayout backing,
        IEnumerable<TData> layoutData,
        ITracker dt,
        bool renderLatestItemsFirst = false
      ) : this(
        backing._container, backing._maskRect, layoutData,
        isHorizontal: backing._scrollRect.horizontal,
        backing._padding,
        spacingInScrollableAxis: backing._spacingInScrollableAxis,
        dt, renderLatestItemsFirst, 
        expandElements: backing._expandElements
      ) {
        backing._scrollRect.onValueChanged.subscribe(dt, _ => updateLayout());
      }

      /// <summary>
      /// Overload for uses when <see cref="DynamicLayout"/> logic is needed without scrollable container.
      /// </summary>
      public Init(
        RectTransform _container, RectTransform _maskRect,
        IEnumerable<TData> layoutData,
        bool isHorizontal, Padding padding, float spacingInScrollableAxis,
        ITracker tracker,
        bool renderLatestItemsFirst = false,
        ExpandElementsRectSizeInSecondaryAxis expandElements = ExpandElementsRectSizeInSecondaryAxis.DontExpand
      ) {
        this._container = _container;
        this._maskRect = _maskRect;
        this.layoutData = layoutData.ToList();
        this.isHorizontal = isHorizontal;
        this.padding = padding;
        this.spacingInScrollableAxis = spacingInScrollableAxis;
        this.renderLatestItemsFirst = renderLatestItemsFirst;
        this.expandElements = expandElements;

        // When we add elements to layout and enable it on the same frame,
        // layout does not work correctly due to rect sizes == 0.
        // Unable to solve this properly. NextFrame is a workaround.
        _container.gameObject.EnsureComponent<OnEnableForwarder>().onEvent.subscribe(tracker,
          _ => onEnable(_container.gameObject)
        );
        tracker.track(clearLayout);

        var mask = _maskRect;

        // We need oncePerFrame() because Unity doesn't allow doing operations like gameObject.SetActive()
        // from OnRectTransformDimensionsChange()
        // oncePerFrame() performs operation in LateUpdate
        // ReSharper disable once LocalVariableHidesMember
        var maskSize = this.maskSize = mask.gameObject.EnsureComponent<OnRectTransformDimensionsChangeForwarder>()
          .rectDimensionsChanged
          .oncePerFrame()
          .filter(_ => mask) // mask can go away before late update, so double check it.
          .toRxVal(() => mask.rect);

        if (isHorizontal && _container.pivot != Vector2.up) {
          Debug.LogError($"Horizontal layout's content should have (0, 1) as pivot, not {_container.pivot}!");
        }

        maskSize.zipSubscribe(containerSizeInScrollableAxis, tracker, (rectSize, size) => {
          Init.onRectSizeChange(container: _container,
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
      public void appendDataIntoLayoutData(TData element, bool updateLayout = true) {       
        layoutData.Add(element);
        if (updateLayout) this.updateLayout();
      }

      /// <param name="elements"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      [PublicAPI]
      public void appendDataIntoLayoutData(IEnumerable<TData> elements, bool updateLayout = true) {
        layoutData.AddRange(elements);
        if (updateLayout) this.updateLayout();
      }

      [PublicAPI]
      public void clearLayoutData() {
        layoutData.Clear();
        clearLayout();
      }
      
      void clearLayout() {
        foreach (var kv in _items) {
          foreach (var item in kv.Value) item.Dispose();
        }
        _items.Clear();
      }

      public Rect calculateVisibleRect => Init.calculateVisibleRectStatic(container: _container, maskRect: _maskRect);

      /// <summary>
      /// You <b>must</b> call this after modifying the underlying data to update the layout so
      /// it would show everything correctly.
      /// </summary>
      [PublicAPI]
      public void updateLayout() {
        var result = forEachElement(
          this, static (data, placementVisible, cellRect, dis) => {
            switch (placementVisible) {
              case true: {
                if (!dis._items.TryGetValue(data, out var instanceOpt)) {
                  {if (data.asViewFactory.toStruct().valueOut(out var elementWithView)) {
                    var instance = elementWithView.createItem(dis._container);
                    instanceOpt = instance.some();  
                    dis._items.Add(data, instanceOpt);
                  }}                  
                }

                {if (instanceOpt.valueOut(out var instance)) {
                  Init.updateVisibleElement(
                    instance, cellRect: cellRect, padding: dis.padding, isHorizontal: dis.isHorizontal,
                    expandElements: dis.expandElements, containerSize: dis._container.rect
                  );
                }}
                break;
              }
              case false when dis._items.ContainsKey(data): {
                var itemOpt = dis._items[data];
                dis._items.Remove(data);
                foreach (var item in itemOpt) {
                  item.Dispose();
                }
                break;
              }
            }
          }
        );
        
        containerSizeInScrollableAxis.value = result.containerSizeInScrollableAxis;
      }
      
      public Option<Percentage> findItemsNormalizedScrollPositionForItem(Func<TData, bool> predicate) {
        var result = Option<Rect>.None;
        var forEachResult = forEachElement(
          predicate, (data, isVisible, cellRect, predicate_) => {
            if (predicate_(data)) {
              result = Some.a(cellRect);
            }
          }
        );
        {if (result.valueOut(out var cellRect)) {
          var viewportSize = _maskRect.rect;
          var scrollPosition = isHorizontal
            ? (cellRect.center.x - viewportSize.width / 2f) / (forEachResult.containerSizeInScrollableAxis - viewportSize.width)
            : 1f - (Mathf.Abs(cellRect.center.y) - viewportSize.height / 2f) / (forEachResult.containerSizeInScrollableAxis - _maskRect.rect.height);
          
          return Some.a(new Percentage(scrollPosition));
        } else {
          return None._;
        }}
      }
      
      public Option<B> findItem<B>(Func<TData, Rect, Option<B>> predicate) {
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
      
      public ImmutableArrayC<A> collectItems<A>(Func<TData, Rect, Option<A>> predicate) {
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
      
      public Option<TView> findVisibleItem(Func<TData, bool> predicate)
      {
        var result = Option<TView>.None;
        forEachElementStoppable(
          predicate, 
          (data, isVisible, cellRect, predicate_) => {
            if (
              isVisible
              && predicate_(data)
              && _items.TryGetValue(data, out var maybeNonSpacerView)
              && maybeNonSpacerView.valueOut(out var view)
            ) {
              result = view.downcast(default(TView));
              return ForEachElementActionResult.StopIterating;
            }
            else {
              return ForEachElementActionResult.ContinueIterating;
            }
          }
        );
        return result;
      }

      /// <inheritdoc cref="Init.forEachElement{TElementData,Data}"/>
      ForEachElementResult forEachElement<Data>(
        Data dataA, ForEachElementAction<TData, Data> updateElement
      ) =>
        Init.forEachElement(
          spacing: spacingInScrollableAxis, iElementDatas: layoutData,
          renderLatestItemsFirst: renderLatestItemsFirst, padding: padding, isHorizontal: isHorizontal,
          containersRectTransform: _container, visibleRect: calculateVisibleRect, dataA: dataA,
          forEachElementAction: updateElement
        );

      /// <inheritdoc cref="Init.forEachElementStoppable{TElementData,Data}"/>
      Option<ForEachElementResult> forEachElementStoppable<Data>(
        Data dataA, ForEachElementActionStoppable<TData, Data> updateElement
      ) =>
        Init.forEachElementStoppable(
          spacing: spacingInScrollableAxis, iElementDatas: layoutData,
          renderLatestItemsFirst: renderLatestItemsFirst, padding: padding, isHorizontal: isHorizontal,
          containersRectTransform: _container, visibleRect: calculateVisibleRect, dataA: dataA,
          forEachElementAction: updateElement
        );
    }
  }
}