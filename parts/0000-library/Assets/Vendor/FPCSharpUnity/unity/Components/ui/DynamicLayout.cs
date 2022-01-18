using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Components.Forwarders;
using FPCSharpUnity.unity.Components.Interfaces;
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
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    #endregion

    /// <summary>
    /// Visual part of layout item.
    /// </summary>
    public interface IElementView : IDisposable {
      RectTransform rectTransform { get; }
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
        backing._container, backing._maskRect, layoutData, backing._scrollRect.horizontal,
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
        bool isHorizontal,
        ITracker dt,
        bool renderLatestItemsFirst = false
      ) {
        this._container = _container;
        this._maskRect = _maskRect;
        this.layoutData = layoutData.ToList();
        this.isHorizontal = isHorizontal;
        this.renderLatestItemsFirst = renderLatestItemsFirst;

        // When we add elements to layout and enable it on the same frame,
        // layout does not work correctly due to rect sizes == 0.
        // Unable to solve this properly. NextFrame is a workaround.
        _container.gameObject.EnsureComponent<OnEnableForwarder>().onEvent.subscribe(dt,
          _ => onEnable(_container.gameObject)
        );
        dt.track(clearLayout);

        var mask = _maskRect;

        // We need oncePerFrame() because Unity doesn't allow doing operations like gameObject.SetActive()
        // from OnRectTransformDimensionsChange()
        // oncePerFrame() performs operation in LateUpdate
        var maskSize = 
          mask.gameObject.EnsureComponent<OnRectTransformDimensionsChangeForwarder>().rectDimensionsChanged
          .oncePerFrame()
          .filter(_ => mask) // mask can go away before late update, so double check it.
          .map(_ => mask.rect)
          .toRxVal(mask.rect);

        var rectTransformAxis = isHorizontal
          ? RectTransform.Axis.Horizontal
          : RectTransform.Axis.Vertical;
        maskSize.zip(containerSizeInScrollableAxis, (_, size) => size).subscribe(dt, size => {
          _container.SetSizeWithCurrentAnchors(rectTransformAxis, size);
          clearLayout();
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
              case true when !dis._items.ContainsKey(data): {
                var instanceOpt = Option<IElementView>.None;
                foreach (var elementWithView in data.asElementWithView) {
                  var instance = elementWithView.createItem(dis._container);
                  var rectTrans = instance.rectTransform;
                  rectTrans.anchorMin = rectTrans.anchorMax = Vector2.up;
                  rectTrans.localPosition = Vector3.zero;
                  rectTrans.anchoredPosition = cellRect.center;
                  instanceOpt = instance.some();
                }
                dis._items.Add(data, instanceOpt);
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
        
        var totalOffsetUntilThisRow = 0f;
        var currentRowSizeInScrollableAxis = 0f;
        var currentSizeInSecondaryAxisPerc = 0f;

        var direction = renderLatestItemsFirst ? -1 : 1;
        for (
          var idx = renderLatestItemsFirst ? layoutData.Count - 1 : 0;
          renderLatestItemsFirst ? idx >= 0 : idx < layoutData.Count;
          idx += direction
        ) {
          var data = layoutData[idx];
          var itemSizeInSecondaryAxisPerc = data.sizeInSecondaryAxis.value;
          var itemLeftPerc = 0f;
          if (currentSizeInSecondaryAxisPerc + itemSizeInSecondaryAxisPerc > 1f + EPS) {
            currentSizeInSecondaryAxisPerc = itemSizeInSecondaryAxisPerc;
            totalOffsetUntilThisRow += currentRowSizeInScrollableAxis;
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
    /// Represents layout data for <see cref="DynamicLayout"/>. When this layout element becomes visible in viewport,
    /// it uses <see cref="GameObjectPool"/> to create it's visual part. You need to override this class for your
    /// specific <see cref="Obj"/> view.
    /// </summary>
    /// <typeparam name="Obj"></typeparam>
    public abstract class ElementWithViewData<Obj> : IElementWithViewData where Obj : Component {
      readonly GameObjectPool<Obj> pool;
      public float sizeInScrollableAxis { get; }
      public Percentage sizeInSecondaryAxis { get; }
      
      public Option<IElementWithViewData> asElementWithView => Some.a<IElementWithViewData>(this);
      
      protected abstract IDisposable setup(Obj view);

      public ElementWithViewData(
        GameObjectPool<Obj> pool, float sizeInScrollableAxis, Percentage sizeInSecondaryAxis
      ) {
        this.pool = pool;
        this.sizeInSecondaryAxis = sizeInSecondaryAxis;
        this.sizeInScrollableAxis = sizeInScrollableAxis;
      }

      public IElementView createItem(Transform parent) {
        var view = pool.borrow();
        return new PooledElementView<Obj>(view, setup(view), pool);
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
      public RectTransform rectTransform { get; }
      
      public PooledElementView(Obj visual, IDisposable disposable, GameObjectPool<Obj> pool) {
        this.visual = visual;
        this.disposable = disposable;
        this.pool = pool;
        rectTransform = (RectTransform) visual.transform;
      }
      public void Dispose() {
        if (visual) pool.release(visual);
        disposable.Dispose();
      }
    }
  }
}