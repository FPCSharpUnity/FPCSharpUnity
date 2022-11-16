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

    /// <summary>
    /// Whether to modify all elements` sizes in secondary axis.
    /// </summary>
    [GenEnumXMLDocConstStrings] public enum ExpandElementsRectSizeInSecondaryAxis {
      /// <summary>
      /// Don't modify all dynamic layout elements sizes in secondary axis to match
      /// <see cref="DynamicLayout._container"/> size.
      /// </summary>
      DontExpand = 0,
      /// <summary>
      /// Modify all dynamic layout elements sizes in secondary axis to match <see cref="DynamicLayout._container"/>
      /// size.
      /// </summary>
      Expand = 1
    }

    [Serializable] public partial class Padding {
      #region Unity Serialized Fields
#pragma warning disable 649
      // ReSharper disable NotNullMemberIsNotInitialized
      [SerializeField, PublicAccessor] float _left, _right, _top, _bottom;
      // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
      #endregion

      public float horizontal => _left + _right;
      public float vertical => _top + _bottom;
    }

    /// <summary>
    /// Visual part of layout item.
    /// </summary>
    public interface IElementView : IDisposable {
      RectTransform rectTransform { get; }
      /// <summary>Item width portion in vertical layout width OR height in horizontal layout.</summary>
      Percentage sizeInSecondaryAxis { get; }
      /// <summary>
      /// Is called when <see cref="DynamicLayout.Init.updateVisibleElement"/> is
      /// called, just before the rect position is set.
      /// </summary>
      void onUpdateLayout(Rect containerSize, Padding padding);
    }

    /// <summary>
    /// Logical part of layout item.
    /// Used to determine layout height and item positions
    /// </summary>
    public interface IElementData {
      /// <summary>Height of an element in a vertical layout OR width in horizontal layout</summary>
      float sizeInScrollableAxis { get; }
      /// <summary>Item width portion of vertical layout width OR height in horizontal layout.</summary>
      Percentage sizeInSecondaryAxis { get; }
      Option<IElementWithViewData> asElementWithView { get; }
    }
    
    public interface IElementWithViewData : IElementData {
      /// <summary>
      /// Function to create a layout item.
      /// It is expected that you take <see cref="IElementView"/> from a pool when <see cref="createItem"/> is called
      /// and release an item to the pool on <see cref="IDisposable.Dispose"/>
      /// </summary>
      IElementView createItem(Transform parent);
    }

    /// <summary>
    /// Empty spacer element
    /// </summary>
    [Record] public partial class EmptyElement : IElementData {
      public float sizeInScrollableAxis { get; }
      public Percentage sizeInSecondaryAxis { get; }
      public Option<IElementWithViewData> asElementWithView => None._;
    }
    
    public class Init {
      const float EPS = 1e-9f;

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
          .map(_ => mask.rect)
          .toRxVal(mask.rect);

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

      /// <summary>
      /// We take new data list and check if there are items with same id, to not recreate the same view.
      /// If it is, we just try to reassign data for this <see cref="IElementView"/>.
      /// If not, we Dispose this <see cref="IElementView"/>.
      /// </summary>
      public void replaceAllElements<TId>(IList<IElementData> elements, Func<IElementData, Option<TId>> getId)
        where TId : IEquatable<TId>
      {
        var elementDatas = new Dictionary<TId, IElementData>();
        foreach (var element in elements) {
          if (getId(element).valueOut(out var id)) {
            elementDatas.Add(id, element);
          }
        }

        var newAssignments = new List<(IElementView view, IElementData newData)>();
        foreach (var kv in _items) {
          {if (kv.Value.valueOut(out var value)) {
            if (
              getId(kv.Key).valueOut(out var id)
              && elementDatas.TryGetValue(id, out var elementData)
              && elementData.asElementWithView.valueOut(out var elementWithView)
              && value.tryToReassignData(elementWithView)
            ) {
              newAssignments.Add((value, elementData));
            }
            else {
              value.Dispose();
            }
          }}
        }

        _items.Clear();
        foreach (var newAssignment in newAssignments) {
          _items.Add(newAssignment.newData, Some.a(newAssignment.view));
        }

        layoutData.Clear();
        layoutData.AddRange(elements);

        updateLayout();
      }
      
      void clearLayout() {
        foreach (var kv in _items) {
          if (kv.Value.valueOut(out var value)) value.Dispose();
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

      public static void updateForEachElementStatic<Data>(
        float spacing, List<IElementData> iElementDatas, bool renderLatestItemsFirst, Padding padding, bool isHorizontal, 
        RectTransform containersRectTransform, Rect visibleRect, Data dataA, 
        UpdateForEachElementAction<Data> updateElement, out float containerSizeInScrollableAxis
      ) {
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
        for (
          var idx = renderLatestItemsFirst ? iElementDatas.Count - 1 : 0;
          renderLatestItemsFirst ? idx >= 0 : idx < iElementDatas.Count;
          idx += direction
        ) {
          var data = iElementDatas[idx];
          var itemSizeInSecondaryAxisPerc = data.sizeInSecondaryAxis.value * secondaryAxisRemapMultiplier;
          var itemLeftPerc = 0f;
          if (currentSizeInSecondaryAxisPerc + itemSizeInSecondaryAxisPerc + paddingPercentageEnd > 1f + EPS) {
            itemLeftPerc = paddingPercentageStart;
            currentSizeInSecondaryAxisPerc = paddingPercentageStart + itemSizeInSecondaryAxisPerc;
            totalOffsetUntilThisRow += currentRowSizeInScrollableAxis + spacing;
            currentRowSizeInScrollableAxis = data.sizeInScrollableAxis;
          }
          else {
            itemLeftPerc = currentSizeInSecondaryAxisPerc;
            currentSizeInSecondaryAxisPerc += itemSizeInSecondaryAxisPerc;
            currentRowSizeInScrollableAxis = Mathf.Max(currentRowSizeInScrollableAxis, data.sizeInScrollableAxis);
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
              width: data.sizeInScrollableAxis,
              height: itemHeight
            );
          }
          else {
            var x = itemLeftPerc * containerWidth;
            cellRect = new Rect(
              x: x,
              y: -totalOffsetUntilThisRow - data.sizeInScrollableAxis,
              width: containerWidth * itemSizeInSecondaryAxisPerc,
              height: data.sizeInScrollableAxis
            );            
          }
             
          var placementVisible = visibleRect.Overlaps(cellRect, true);

          updateElement(data, placementVisible, cellRect, dataA);
        }

        totalOffsetUntilThisRow += isHorizontal ? padding.right : padding.bottom;
        containerSizeInScrollableAxis = totalOffsetUntilThisRow + currentRowSizeInScrollableAxis;
      }
    }

    /// <summary>
    /// Useful when you need a not pooled <see cref="DynamicLayout"/> element, the one that just can be turned on/off
    /// directly in scene, when <see cref="rectTransform"/> becomes visible in viewport.
    /// </summary>
    public class NonPooledRectTransformElementWithViewData : IElementWithViewData, IElementView {
      public RectTransform rectTransform { get; }
      public float sizeInScrollableAxis { get; }
      public Percentage sizeInSecondaryAxis { get; }

      public Option<IElementWithViewData> asElementWithView => Some.a<IElementWithViewData>(this);
      
      public virtual void onUpdateLayout(Rect containerSize, Padding padding) {}
      
      public NonPooledRectTransformElementWithViewData(
        RectTransform rectTransform, float sizeInScrollableAxis, Percentage sizeInSecondaryAxis
      ) {
        this.rectTransform = rectTransform;
        this.sizeInSecondaryAxis = sizeInSecondaryAxis;
        this.sizeInScrollableAxis = sizeInScrollableAxis;
        // Pooled elements starts with disposed state, so we need to do the same for non pooled elements too:
        Dispose();
      }

      public IElementView createItem(Transform parent) {
        rectTransform.setActiveGO(true);
        return this;
      }
      
      public void Dispose() {
        rectTransform.setActiveGO(false);
      }
    }

    /// <summary>
    /// It's a callback when <see cref="Init.updateLayout"/> is called. This happens before setting item's position in
    /// <see cref="DynamicLayout._container"/>.
    /// </summary>
    /// <typeparam name="Obj">A view type.</typeparam>
    public delegate void OnUpdateLayout<in Obj>(
      Obj view, Rect viewportSize, RectTransform viewRt, Padding padding
    ) where Obj : Component;
    
    /// <summary>
    /// Represents layout data for <see cref="DynamicLayout"/>. When this layout element becomes visible in viewport,
    /// it uses <see cref="GameObjectPool"/> to create it's visual part. You need to override this class for your
    /// specific <see cref="Obj"/> view.
    /// </summary>
    /// <typeparam name="Obj"></typeparam>
    public abstract class ElementWithViewData<Obj> : IElementWithViewData where Obj : Component {
      readonly GameObjectPool<Obj> pool;
      [CanBeNull] readonly OnUpdateLayout<Obj> maybeOnUpdateLayout;
      public float sizeInScrollableAxis { get; set; }
      public Percentage sizeInSecondaryAxis { get; }
      
      public Option<IElementWithViewData> asElementWithView => Some.a<IElementWithViewData>(this);
      
      protected abstract IDisposable setup(Obj view);

      public ElementWithViewData(
        GameObjectPool<Obj> pool, float sizeInScrollableAxis, Percentage sizeInSecondaryAxis, 
        [CanBeNull] OnUpdateLayout<Obj> maybeOnUpdateLayout = null
      ) {
        this.pool = pool;
        this.maybeOnUpdateLayout = maybeOnUpdateLayout;
        this.sizeInSecondaryAxis = sizeInSecondaryAxis;
        this.sizeInScrollableAxis = sizeInScrollableAxis;
      }

      public IElementView createItem(Transform parent) {
        var view = pool.borrow();
        return new PooledElementView<Obj>(view, setup(view), pool, maybeOnUpdateLayout: maybeOnUpdateLayout,
          sizeInSecondaryAxis: sizeInSecondaryAxis
        );
      }
    }
    
    /// <summary>
    /// Visual part of <see cref="ElementWithViewData{Obj}"/>. When item becomes hidden (outside of viewport) it gets
    /// returned to <see cref="GameObjectPool"/>.
    /// </summary>
    /// <typeparam name="Obj"></typeparam>
    public class PooledElementView<Obj> : IElementView where Obj : Component {
      readonly Obj visual;
      readonly IDisposable disposable;
      readonly GameObjectPool<Obj> pool;
      [CanBeNull] readonly OnUpdateLayout<Obj> maybeOnUpdateLayoutFunc;
      public RectTransform rectTransform { get; }
      public Percentage sizeInSecondaryAxis { get; }

      public virtual void onUpdateLayout(Rect containerSize, Padding padding) {
        maybeOnUpdateLayoutFunc?.Invoke(visual, containerSize, rectTransform, padding);
      }
      
      public PooledElementView(
        Obj visual, IDisposable disposable, GameObjectPool<Obj> pool, OnUpdateLayout<Obj> maybeOnUpdateLayout,
        Percentage sizeInSecondaryAxis
      ) {
        this.sizeInSecondaryAxis = sizeInSecondaryAxis;
        this.visual = visual;
        this.disposable = disposable;
        this.pool = pool;
        rectTransform = (RectTransform) visual.transform;
        maybeOnUpdateLayoutFunc = maybeOnUpdateLayout;
      }
      public void Dispose() {
        if (visual) pool.release(visual);
        disposable.Dispose();
      }
    }
  }
}