using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Components.Forwarders;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Pools;
using FPCSharpUnity.unity.Reactive;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  /// <summary>
  /// Scrollable vertical/horizontal layout, which makes sure that only visible elements are created.
  /// Element is considered visible if it intersects with <see cref="_maskRect"/> bounds.
  ///
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
    [SerializeField, NotNull] float _spacing;
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    #endregion

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
      void onUpdateLayout(Rect viewportSize, Padding padding);
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
    public class EmptyElement : IElementData {
      public float sizeInScrollableAxis { get; }
      public Percentage sizeInSecondaryAxis { get; }
      public Option<IElementWithViewData> asElementWithView => None._;
      
      public EmptyElement(float sizeInScrollableAxis, Percentage sizeInSecondaryAxis) {
        this.sizeInScrollableAxis = sizeInScrollableAxis;
        this.sizeInSecondaryAxis = sizeInSecondaryAxis;
      }
    }
    
    public class Init {
      const float EPS = 1e-9f;

      readonly RectTransform _container, _maskRect;
      readonly List<IElementData> layoutData;
      readonly IRxRef<float> containerSizeInScrollableAxis = RxRef.a(0f);
      readonly bool renderLatestItemsFirst;
      readonly bool isHorizontal;
      readonly Padding padding;
      readonly float spacing;
      public readonly IRxVal<Rect> maskSize;

      // If Option is None, that means there is no backing view, it is only a spacer.
      readonly IDictionary<IElementData, Option<IElementView>> _items = 
        new Dictionary<IElementData, Option<IElementView>>();

      public Option<Option<IElementView>> get(IElementData key) => _items.get(key);
      
      // When we add elements to layout and enable it on the same frame,
      // layout does not work correctly due to rect sizes == 0.
      // Unable to solve this properly. NextFrame is a workaround. 
      void onEnable(GameObject gameObject) => ASync.NextFrame(gameObject, updateLayout);

      public Init(
        DynamicLayout backing,
        IEnumerable<IElementData> layoutData,
        ITracker dt,
        bool renderLatestItemsFirst = false
      ) : this(
        backing._container, backing._maskRect, layoutData,
        isHorizontal: backing._scrollRect.horizontal,
        backing._padding,
        spacing: backing._spacing,
        dt, renderLatestItemsFirst
      ) {
        backing._scrollRect.onValueChanged.subscribe(dt, _ => updateLayout());
      }

      /// <summary>
      /// Overload for uses when <see cref="DynamicLayout"/> logic is needed without scrollable container.
      /// </summary>
      public Init(
        RectTransform _container, RectTransform _maskRect,
        IEnumerable<IElementData> layoutData,
        bool isHorizontal, Padding padding, float spacing,
        ITracker tracker,
        bool renderLatestItemsFirst = false
      ) {
        this._container = _container;
        this._maskRect = _maskRect;
        this.layoutData = layoutData.ToList();
        this.isHorizontal = isHorizontal;
        this.padding = padding;
        this.spacing = spacing;
        this.renderLatestItemsFirst = renderLatestItemsFirst;

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

        var rectTransformAxis = isHorizontal
          ? RectTransform.Axis.Horizontal
          : RectTransform.Axis.Vertical;
        maskSize.zipSubscribe(containerSizeInScrollableAxis, tracker, (_, size) => {
          _container.SetSizeWithCurrentAnchors(rectTransformAxis, size);
          updateLayout();
        });
      }

      /// <param name="element"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      [PublicAPI]
      public void appendDataIntoLayoutData(IElementData element, bool updateLayout = true) {       
        layoutData.Add(element);
        if (updateLayout) this.updateLayout();
      }

      /// <param name="elements"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      [PublicAPI]
      public void appendDataIntoLayoutData(IEnumerable<IElementData> elements, bool updateLayout = true) {
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
      
      public Rect calculateVisibleRect => _maskRect.rect.convertCoordinateSystem(
        ((Transform) _maskRect).some(), _container
      );

      /// <summary>
      /// You can call this after modifying the underlying data to update the layout so
      /// it would show everything correctly.
      /// </summary>
      [PublicAPI]
      public void updateLayout() {
        updateForEachElement(
          this, static (data, placementVisible, cellRect, dis) => {
            switch (placementVisible) {
              case true: {
                if (!dis._items.TryGetValue(data, out var instanceOpt)) {
                  {if (data.asElementWithView.valueOut(out var elementWithView)) {
                    var instance = elementWithView.createItem(dis._container);
                    instanceOpt = instance.some();  
                    dis._items.Add(data, instanceOpt);
                  }}                  
                }
                
                {if (instanceOpt.valueOut(out var instance)) {
                  // Call this first, because in there could be code which resizes this item's rectTransform.
                  instance.onUpdateLayout(dis.maskSize.value, dis.padding);
                  
                  var rectTrans = instance.rectTransform;
                  rectTrans.anchorMin = rectTrans.anchorMax = Vector2.up;
                  rectTrans.localPosition = Vector3.zero;
                  rectTrans.anchoredPosition = cellRect.center;    
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
          }, 
          out var containerSizeInScrollableAxis_
        );
        
        containerSizeInScrollableAxis.value = containerSizeInScrollableAxis_;
      }

      /// <summary>
      /// Find normalized position of an item for scrolling to. If you use this position to scroll ScrollRect, the
      /// item will be in the center of viewport (until you reach the content ends, then the scroll is clamped)
      /// </summary>
      public Option<float> findItemsNormalizedScrollPositionForItem(Func<IElementData, bool> predicate) {
        var result = Option<Rect>.None;
        updateForEachElement(
          predicate, (data, isVisible, cellRect, predicate_) => {
            if (predicate_(data)) {
              result = Some.a(cellRect);
            }
          }, out var containerSizeInScrollableAxis_
        );
        {if (result.valueOut(out var cellRect)) {
          var viewportSize = _maskRect.rect;
          var scrollPosition = isHorizontal
            ? (cellRect.center.x - viewportSize.width / 2f) / (containerSizeInScrollableAxis_ - viewportSize.width)
            : 1f - (Mathf.Abs(cellRect.center.y) - viewportSize.height / 2f) / (containerSizeInScrollableAxis_ - _maskRect.rect.height);
          
          return Some.a(scrollPosition);
        } else {
          return None._;
        }}
      }
      
      /// <summary>
      /// Find item cell rect. This is useful when we want to get the position of an item even if it is not visible.
      /// </summary>
      public Option<Rect> findItemRect(Func<IElementData, bool> predicate) {
        var result = Option<Rect>.None;
        updateForEachElement(
          predicate, 
          (data, isVisible, cellRect, predicate_) => {
            if (result.isNone && predicate_(data)) {
              result = Some.a(cellRect);
            }
          }, 
          out _
        );
        return result;
      }
      
      public delegate void UpdateForEachElementAction<in Data>(
        IElementData elementData, bool placementVisible, Rect cellRect, Data data
      );
      
      void updateForEachElement<Data>(
        Data dataA, UpdateForEachElementAction<Data> updateElement, out float containerSizeInScrollableAxis_
      ) {
        var visibleRect = calculateVisibleRect;
        var containerRect = _container.rect;
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
          var idx = renderLatestItemsFirst ? layoutData.Count - 1 : 0;
          renderLatestItemsFirst ? idx >= 0 : idx < layoutData.Count;
          idx += direction
        ) {
          var data = layoutData[idx];
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
        containerSizeInScrollableAxis_ = totalOffsetUntilThisRow + currentRowSizeInScrollableAxis;
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
      
      public virtual void onUpdateLayout(Rect viewportSize, Padding padding) {}
      
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
        return new PooledElementView<Obj>(view, setup(view), pool, maybeOnUpdateLayout: maybeOnUpdateLayout);
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

      public virtual void onUpdateLayout(Rect viewportSize, Padding padding) {
        maybeOnUpdateLayoutFunc?.Invoke(visual, viewportSize, rectTransform, padding);
      }
      
      public PooledElementView(
        Obj visual, IDisposable disposable, GameObjectPool<Obj> pool, OnUpdateLayout<Obj> maybeOnUpdateLayout
      ) {
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