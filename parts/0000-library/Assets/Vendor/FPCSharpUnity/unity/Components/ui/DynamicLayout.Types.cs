using System;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Pools;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  /// <summary>
  /// Whether to modify all elements` sizes in secondary axis.
  /// </summary>
  [GenEnumXMLDocConstStrings]
  public enum ExpandElementsRectSizeInSecondaryAxis {
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

  [Serializable]
  public partial class Padding {
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
  /// Visual part of layout item. It gets created after it moved inside viewport.
  /// </summary>
  public interface IElementView : IDisposable {
    RectTransform rectTransform { get; }

    /// <summary>Item width portion in vertical layout width OR height in horizontal layout.</summary>
    Percentage sizeInSecondaryAxis { get; }

    /// <summary>
    /// Is called when <see cref="Init.updateVisibleElement"/> is
    /// called, just before the rect position is set.
    /// </summary>
    void onUpdateLayout(Rect containerSize, Padding padding);
  }

  /// <summary>
  /// Logical part of layout item.
  /// <para/>
  /// Used to determine layout height and item positions
  /// </summary>
  public interface IElementDataForLayout {
    /// <summary>Height of an element in a vertical layout OR width in horizontal layout</summary>
    float sizeInScrollableAxis { get; }

    /// <summary>Item width portion of vertical layout width OR height in horizontal layout.</summary>
    Percentage sizeInSecondaryAxis { get; }
  }

  /// <summary>
  /// <see cref="IElementDataForLayout"/> plus maybe a way to render this element.
  /// </summary>
  public interface IElementData<out TView> : IElementDataForLayout where TView : IElementView {
    /// <summary>The element is invisible if this returns `None`.</summary>
    COption<IViewFactory<TView>> asViewFactory { get; }
  }

  /// <summary> Knows how to create a <see cref="TView"/>. </summary>
  public interface IViewFactory<out TView> where TView : IElementView {
    /// <summary>
    /// Function to create a layout item.
    /// It is expected that you take <see cref="IElementView"/> from a pool when <see cref="createItem"/> is called
    /// and release an item to the pool on <see cref="IDisposable.Dispose"/>
    /// </summary>
    TView createItem(Transform parent);
  }

  /// <summary>
  /// Empty spacer element.
  /// <para/>
  /// If you want to add spaces before, after and between all elements inside <see cref="DynamicLayout"/>, prefer to
  /// change serialized fields <see cref="_spacingInScrollableAxis"/> and <see cref="_padding"/> instead of adding
  /// this element inside the layout.
  /// <para/>
  /// Only use this if you want to have different spacings between items, than <see cref="_spacingInScrollableAxis"/>
  /// provides.
  /// </summary>
  [Record]
  public partial class EmptyElement : IElementData<IElementView> {
    public float sizeInScrollableAxis { get; }
    public Percentage sizeInSecondaryAxis { get; }
    public COption<IViewFactory<IElementView>> asViewFactory => CNone<IViewFactory<IElementView>>._;
  }

  /// <summary>
  /// Layout part of <see cref="Init{TData,TView}"/>.
  /// </summary>
  public interface ILayout {
    /// <inheritdoc cref="Init.calculateVisibleRectStatic"/>
    Rect calculateVisibleRect { get; }
  }

  /// <summary>
  /// Container part of <see cref="Init{TData,TView}"/>.
  /// </summary>
  /// <typeparam name="TData">Type of data held by the container.</typeparam>
  public interface IElements<out TData> {
    /// <summary>
    /// Find item cell rect. This is useful when we want to get the position of an item even if it is not visible.
    /// </summary>
    Option<B> findItem<B>(Func<TData, Rect, Option<B>> predicate);

    /// <summary>
    /// Find item cell rects. This is useful when we want to get the position of items even if they are not visible.
    /// </summary>
    ImmutableArrayC<A> collectItems<A>(Func<TData, Rect, Option<A>> predicate);

    /// <summary>
    /// Find normalized position of an item for scrolling to. If you use this position to scroll ScrollRect, the
    /// item will be in the center of viewport (until you reach the content ends, then the scroll is clamped)
    /// </summary>
    Option<Percentage> findItemsNormalizedScrollPositionForItem(Func<TData, bool> predicate);
  }

  /// <summary>
  /// Actions with currently visible items inside layout.
  /// </summary>
  public interface IVisibleElements<out TData, TView> {
    /// <summary>
    /// Finds currently visible item by type.
    /// </summary>
    /// <param name="predicate">Filter to check if item is the one we need.</param>
    /// <returns></returns>
    public Option<TView> findVisibleItem(Func<TData, bool> predicate);
  }
  
  /// <summary> Action that gets called on each <see cref="DynamicLayout"/> element. </summary>
  public delegate void ForEachElementAction<in TElementData, in Data>(
    TElementData elementData, bool placementVisible, Rect cellRect, Data data
  );

  /// <summary>
  /// Result type for <see cref="ForEachElementActionStoppable{TElementData,Data}"/>.
  /// </summary>
  public enum ForEachElementActionResult {
    /// <summary>Do not go through the rest of the elements in the layout.</summary>
    StopIterating,
    
    /// <summary>Go to the next element in the layout.</summary>
    ContinueIterating
  }

  /// <summary> Action that gets called on each <see cref="DynamicLayout"/> element. </summary>
  public delegate ForEachElementActionResult ForEachElementActionStoppable<in TElementData, in Data>(
    TElementData elementData, bool placementVisible, Rect cellRect, Data data
  );

  /// <summary>Result type for <see cref="Init.forEachElementStoppable{TElementData,Data}"/>.</summary>
  [Record] public readonly partial struct ForEachElementResult {
    /// <inheritdoc cref="Init{TData,TView}.containerSizeInScrollableAxis"/>
    public readonly float containerSizeInScrollableAxis;
  }  

  /// <summary>
  /// Useful when you need a not pooled <see cref="DynamicLayout"/> element, the one that just can be turned on/off
  /// directly in scene, when <see cref="rectTransform"/> becomes visible in viewport.
  /// </summary>
  public class NonPooledRectTransformElementWithViewData : 
    IElementData<IElementView>, IViewFactory<IElementView>, IElementView 
  {
    readonly Action<bool> setActive;
    public RectTransform rectTransform { get; }
    public float sizeInScrollableAxis { get; }
    public Percentage sizeInSecondaryAxis { get; }

    [LazyProperty] public COption<IViewFactory<IElementView>> asViewFactory => CSome.a(this);
      
    public virtual void onUpdateLayout(Rect containerSize, Padding padding) {}
      
    public NonPooledRectTransformElementWithViewData(
      RectTransform rectTransform, float sizeInScrollableAxis, Percentage sizeInSecondaryAxis,
      [CanBeNull] Action<bool> setActiveOverride = null
    ) {
      this.rectTransform = rectTransform;
      this.sizeInSecondaryAxis = sizeInSecondaryAxis;
      this.sizeInScrollableAxis = sizeInScrollableAxis;
      setActive = setActiveOverride ?? (b => rectTransform.setActiveGO(b));
      // Pooled elements starts with disposed state, so we need to do the same for non pooled elements too:
      Dispose();
    }

    public virtual IElementView createItem(Transform parent) {
      setActive(true);
      return this;
    }
      
    public void Dispose() {
      setActive(false);
    }
  }

  /// <summary>
  /// It's a callback when <see cref="Init{TData,TView}.updateLayout"/> is called.
  /// <para/>
  /// This happens before setting item's position in <see cref="DynamicLayout._container"/>.
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
  public abstract class ElementWithViewData<Obj> : 
    IElementData<IElementView>, IViewFactory<IElementView> 
    where Obj : Component 
  {
    readonly GameObjectPool<Obj> pool;
    [CanBeNull] readonly OnUpdateLayout<Obj> maybeOnUpdateLayout;
    public float sizeInScrollableAxis { get; set; }
    public Percentage sizeInSecondaryAxis { get; }
      
    [LazyProperty] public COption<IViewFactory<IElementView>> asViewFactory => CSome.a(this);
      
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

  /// <summary>
  /// Helper method to easily create <see cref="SimpleDynamicLayoutElement{Obj,Data}"/> elements.
  /// </summary>
  public static SimpleDynamicLayoutElement<Obj, Data> createSimplePooledElement<Obj, Data>(
    Obj template, GameObjectPool<Obj> pool, Data itemData, bool isHorizontal,
    Action<Obj, Data, ITracker> setupAction, Percentage? sizeInSecondaryAxisOverride = null,
    [Implicit] ILog log = null
  ) where Obj : Component =>
    new(template, pool, itemData, isHorizontal, setupAction, sizeInSecondaryAxisOverride, log);

  public interface ISimpleDynamicLayoutElement<out Data> {
    Data itemData { get; }
  }

  /// <summary>
  /// This class saves us from creating a separate class for each <see cref="DynamicLayout"/> element we want to show
  /// in a list. It provides everything through 'setup' method. Use this for pooled elements. Less boilerplate!
  /// </summary>
  public class SimpleDynamicLayoutElement<Obj, Data> : ElementWithViewData<Obj>, ISimpleDynamicLayoutElement<Data>
    where Obj : Component 
  {
    public Data itemData { get; }
    readonly Action<Obj, Data, ITracker> setupAction;
    [Implicit] readonly ILog log;

    public SimpleDynamicLayoutElement(
      Obj template, GameObjectPool<Obj> pool, Data itemData, bool isHorizontal, 
      Action<Obj, Data, ITracker> setupAction, Percentage? sizeInSecondaryAxisOverride = null,
      [Implicit] ILog log = null
    ) : base(
      pool, 
      sizeInScrollableAxis: 
      isHorizontal
        ? ((RectTransform)template.transform).rect.width
        : ((RectTransform)template.transform).rect.height, 
      sizeInSecondaryAxis : sizeInSecondaryAxisOverride ?? Percentage.oneHundred
    ) {
      this.itemData = itemData;
      this.setupAction = setupAction;
      this.log = log;
    }

    protected override IDisposable setup(Obj view) {
      var tracker = new DisposableTracker(log);
      setupAction(view, itemData, tracker);
      return tracker;
    }
  }

  /// <summary>
  /// Helper method to easily create <see cref="SimpleDynamicLayoutElementNonPooled{Obj,Data}"/> elements.
  /// </summary>
  public static SimpleDynamicLayoutElementNonPooled<Obj, Data> createSimpleNonPooledElement<Obj, Data>(
    Obj item, Data itemData, bool isHorizontal, 
    Action<Obj, Data, ITracker> setupAction, 
    Percentage? sizeInSecondaryAxisOverride = null,
    [CanBeNull] Action<Obj, bool> setActiveOverride = null,
    [Implicit] ILog log = null
  ) where Obj : Component =>
    new(item, itemData, isHorizontal, setupAction, sizeInSecondaryAxisOverride, setActiveOverride, log);

  /// <summary>
  /// This class saves us from creating a separate class for each <see cref="DynamicLayout"/> element we want to show
  /// in a list. It provides everything through 'setup' method. Use this for non pooled elements. Less boilerplate!
  /// </summary>
  public class SimpleDynamicLayoutElementNonPooled<Obj, Data> : IViewFactory<IElementView>, IElementView 
    where Obj : Component 
  {
    readonly Obj item;
    readonly Data itemData;
    readonly Action<Obj, Data, ITracker> setupAction;
    readonly Action<bool> setActive;
    readonly IDisposableTracker tracker;
      
    public RectTransform rectTransform { get; }
    public float sizeInScrollableAxis { get; }
    public Percentage sizeInSecondaryAxis { get; }

    [LazyProperty] public COption<IViewFactory<IElementView>> asElementWithView => CSome.a(this);
      
    public virtual void onUpdateLayout(Rect containerSize, Padding padding) {}
      
    public SimpleDynamicLayoutElementNonPooled(
      Obj item, Data itemData, bool isHorizontal, 
      Action<Obj, Data, ITracker> setupAction, 
      Percentage? sizeInSecondaryAxisOverride = null,
      [CanBeNull] Action<Obj, bool> setActiveOverride = null,
      [Implicit] ILog log = null
    ) {
      this.item = item;
      this.itemData = itemData;
      this.setupAction = setupAction;
      var rectTransform = this.rectTransform = (RectTransform)item.transform;
      sizeInSecondaryAxis = sizeInSecondaryAxisOverride ?? Percentage.oneHundred;
      sizeInScrollableAxis = isHorizontal
        ? ((RectTransform)item.transform).rect.width
        : ((RectTransform)item.transform).rect.height;
      setActive = setActiveOverride != null ? b => setActiveOverride(item, b) : b => rectTransform.setActiveGO(b);
      tracker = new DisposableTracker(log);
      // Pooled elements starts with disposed state, so we need to do the same for non pooled elements too:
      Dispose();
    }

    public IElementView createItem(Transform parent) {
      setActive(true);
      setupAction(item, itemData, tracker);
      return this;
    }
      
    public void Dispose() {
      tracker.Dispose();
      setActive(false);
    }
  }
}