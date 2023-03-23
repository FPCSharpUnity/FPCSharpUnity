using System;
using System.Collections.Generic;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Pools;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  public partial class DynamicLayout {
    /// <summary> Contains main parts and behaviours of <see cref="DynamicLayout"/>. </summary>
    public interface IDynamicLayout : IDynamicLayoutDirection {
      ScrollRect scrollRect { get; }
    
      /// <summary> Container for instantiated items. </summary>
      RectTransform container { get; }
    
      /// <summary> Viewport of <see cref="scrollRect"/>. </summary>
      RectTransform maskRect { get; }
    
      /// <inheritdoc cref="Padding"/>
      Padding padding { get; set; }
    
      /// <summary> Space between each new line/column. </summary>
      float spacingInScrollableAxis { get; }
    
      /// <inheritdoc cref="ExpandElementsRectSizeInSecondaryAxis"/>
      ExpandElementsRectSizeInSecondaryAxis expandElements { get; }
    }
    
    /// <summary> <see cref="DynamicLayout"/>'s scroll and items placement behaviour. </summary>
    public interface IDynamicLayoutDirection {
      /// <summary>
      /// True - the scrollRect is set to horizontal only and items gets placed in columns.<br/>
      /// False - the scrollRect is set to vertical only and items gets placed in rows.
      /// </summary>
      bool isHorizontal { get; }      
    }
  
    /// <summary> Whether to modify all elements` sizes in secondary axis. </summary>
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

    /// <summary> Padding for items that gets placed inside <see cref="container"/>. </summary>
    [Serializable, Record(ConstructorFlags.Withers | ConstructorFlags.Copy)] public partial class Padding {
      #region Unity Serialized Fields
#pragma warning disable 649
      // ReSharper disable NotNullMemberIsNotInitialized
      [SerializeField, PublicAccessor] public float _left, _right, _top, _bottom;
      // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
      #endregion

      public float horizontal => _left + _right;
      public float vertical => _top + _bottom;
    }

    /// <summary> Layout part of <see cref="Init{A}"/>. </summary>
    [PublicAPI] public interface ILayout<in TData> where TData : IElement {
      /// <inheritdoc cref="Init.calculateVisibleRectStatic"/>
      Rect calculateVisibleRect { get; }

      /// <summary>
      /// You <b>must</b> call this after modifying the underlying data to update the layout so
      /// it would show everything correctly.
      /// </summary>
      public void updateLayout();
    
      /// <summary> Container for all layout items we want to place them in. </summary>
      RectTransform elementsParent { get; }

      /// <param name="element"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      public void appendDataIntoLayoutData(TData element, bool updateLayout = true);

      /// <param name="elements"></param>
      /// <param name="updateLayout">
      /// pass false and then call <see cref="updateLayout"/> manually when doing batch updates
      /// </param>
      public void appendDataIntoLayoutData(IEnumerable<TData> elements, bool updateLayout = true);

      /// <summary> Clear layout elements and remove their visuals. </summary>
      public void clearLayoutData();
    }

    public interface IModifyElementsList<TData> : ILayout<TData> where TData : IElement {
      /// <summary>
      /// All layout elements that are present in this layout. This is only exposed for advanced handling of elements,
      /// like sorting/updating. For normal use, consider using
      /// <see cref="Init{CommonDataType}.appendDataIntoLayoutData(CommonDataType,bool)"/>,
      /// <see cref="Init{CommonDataType}.clearLayoutData"/> and <see cref="Init{CommonDataType}.updateLayout"/>
      /// instead.
      /// </summary>
      List<TData> items { get; }    
    }
  
    /// <summary> Container part of <see cref="Init{CommonDataType}"/>. </summary>
    /// <typeparam name="TData">Type of data held by the container.</typeparam>
    public interface IElements<TData> where TData : IElement {
      /// <summary>
      /// Find item cell rect. This is useful when we want to get the position of an item even if it is not visible.
      /// </summary>
      Option<B> findItem<B>(Init<TData>.FindItemPredicate<B> predicate);

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
      /// <inheritdoc cref="Init{TData}.containerSizeInScrollableAxis"/>
      public readonly float containerSizeInScrollableAxis;
    }  

    /// <summary>
    /// It's a callback when <see cref="Init{TData}.updateLayout"/> is called.
    /// <para/>
    /// This happens before setting item's position in <see cref="DynamicLayout._container"/>.
    /// </summary>
    /// <typeparam name="Obj">A view type.</typeparam>
    public delegate void OnUpdateLayout<in Obj>(
      Obj view, Rect viewportSize, RectTransform viewRt, Padding padding
    ) where Obj : Component;
 
    /// <summary>
    /// <see cref="DynamicLayout"/> element data. It is put inside <see cref="DynamicLayout._container"/>. If this item
    /// is moved inside viewport, it can show a UI visual.
    /// </summary>
    public interface IElement {
      /// <inheritdoc cref="SizeProvider"/>
      SizeProvider sizeProvider { get; }
    
      /// <summary>
      /// Will start showing item's visual if it supports it. Repeated calls to this method will result in refreshing
      /// visual with newest data.
      /// </summary>
      /// <param name="parent">See <see cref="DynamicLayout.ILayout{TData}.elementsParent"/>.</param>
      /// <param name="forceUpdate">
      /// Will not update visuals if they were already visible. You can pass `true` here if you want to force run setup
      /// method.
      /// </param>
      /// <returns>
      /// `Some(visual's <see cref="RectTransform"/>)` - if item visual is visible and supported.<br/>
      /// `None` - item doesn't support visual (it's an empty item).
      /// </returns>
      Option<RectTransform> showOrUpdate(RectTransform parent, bool forceUpdate);
    
      /// <summary> Disposes of previously visible item visual (hides it). </summary>
      void hide();
    
      /// <inheritdoc cref="DynamicLayout.OnUpdateLayout{A}"/>
      void onUpdateLayout(Rect containerSize, Padding padding);
    
      /// <summary> Whether this item is visible inside viewport. </summary>
      abstract bool isVisible { get; }
    
      /// <summary>
      /// `Some(visual's <see cref="RectTransform"/>)` - <inheritdoc cref="isVisible"/>.<br/>
      /// `None` - item is not visible inside viewport or doesn't support visuals at all.
      /// </summary>
      abstract Option<RectTransform> visibleRt { get; }
    
      /// <inheritdoc cref="DynamicLayout.ISizeProvider.sizeInSecondaryAxis"/>
      Percentage sizeInSecondaryAxis { get; }
    
      /// <inheritdoc cref="DynamicLayout.ISizeProvider.sizeInScrollableAxis"/>
      float sizeInScrollableAxis(bool isHorizontal);
    }

    /// <summary>
    /// It's useful when you don't want to pass enormous amount of setup data inside the item, and instead you pass setup
    /// action. This allows us to automatically capture all data needed for setup inside closure class.
    /// </summary>
    public abstract class ElementBaseWithSetupDelegate<InnerData, View> : ElementBase<InnerData, View> {
      readonly Action<InnerData, View, ITracker> updateStateDelegate;

      protected ElementBaseWithSetupDelegate(
        InnerData data, SizeProvider sizeProvider, 
        Action<InnerData, View, ITracker> updateStateDelegate,
        [CanBeNull] IViewProvider<View> maybeViewProvider = default, ILog log = default
      ) : base(data, sizeProvider, maybeViewProvider, log) {
        this.updateStateDelegate = updateStateDelegate;
      }

      protected override void updateState(View view, ITracker tracker) => updateStateDelegate(data, view, tracker);
    }
  
    /// <summary> Item is already placed inside scene/prefab and we don't need to setup it. </summary>
    public abstract class ElementBaseForRectTransform : ElementBase<Unit, RectTransform> {
      protected ElementBaseForRectTransform(
        RectTransform rectTransform, 
        Percentage? noneHundredSizeInSecondaryAxis = default,
        [Implicit] ILog log = default
      ) : base(
        Unit._, 
        sizeProvider: new SizeProvider.FromTemplateStatic(
          rectTransform, sizeInSecondaryAxis: noneHundredSizeInSecondaryAxis ?? new Percentage(1)
        ), maybeViewProvider: ViewProvider.singleInstance(rectTransform), log) {}

      protected override void updateState(RectTransform view, ITracker tracker){}
    }
  
    /// <summary> An empty space, which doesn't need any data and no setup. </summary>
    public abstract class ElementBaseForSpacer : ElementBase<Unit, Unit> {
      protected ElementBaseForSpacer(SizeProvider sizeProvider, [Implicit] ILog log = default) 
        : base(Unit._, sizeProvider: sizeProvider, maybeViewProvider: null, log) {}

      protected override void updateState(Unit view, ITracker tracker){}
    }
  
    /// <summary>
    /// Very common case for:
    /// <list>
    /// <item>Item's visuals are pooled.</item>
    /// <item>Visuals takes up full row/column.</item>
    /// <item>We can sample it's width/height from template visual</item>
    /// </list>
    /// </summary>
    public abstract class ElementBaseForPooledRow<InnerData, View> : ElementBase<InnerData, View> where View : Component {
      protected ElementBaseForPooledRow(
        InnerData data, View template, GameObjectPool<View> pool, 
        [Implicit] ILog log = default
      ) : base(
        data, sizeProvider: SizeProvider.FromTemplateStatic.fullRowOrColumn(template), 
        maybeViewProvider: new ViewProvider.Pooled<View>(pool), log
      ) {}
    }

    /// <summary> DynamicLayout element which has a data field. </summary>
    /// <typeparam name="InnerData">Data used for resolving Ui visual in <see cref="DynamicLayout"/>.</typeparam>
    public interface ElementWithInnerData<out InnerData> {
      /// <summary> Data used for resolving item's UI visual in <see cref="DynamicLayout"/>. </summary>
      InnerData data { get; }
    }
  
    /// <summary> Base class for all <see cref="IElement"/> implementations. </summary>
    /// <typeparam name="InnerData">See <see cref="data"/>.</typeparam>
    /// <typeparam name="View">Visual type. If item is visible, it will be Unity object.</typeparam>
    public abstract partial class ElementBase<InnerData, View> : IElement, ElementWithInnerData<InnerData> {
      /// <summary> Tracks currently visible visual. </summary>
      readonly IDisposableTracker tracker;
    
      /// <summary>
      /// Data which contains everything needed for updating it's visuals. Can be mutated for updating visuals without
      /// clearing all items from layout.
      /// </summary>
      public InnerData data { get; protected set; }
    
      /// <summary>
      /// Some(visual provider) - the item will be visible inside viewport.<br/>
      /// None - the item will never be rendered.
      /// </summary>
      protected Option<IViewProvider<View>> maybeViewProvider;
    
      public SizeProvider sizeProvider { get; }
      
      public Percentage sizeInSecondaryAxis => sizeProvider.foldM(
        onFromTemplateWithCustomSizeInSecondaryAxis: static a => a.sizeInSecondaryAxis,
        onStatic: static a => a.sizeInSecondaryAxis,
        onFromTemplateStatic: static a => a.sizeInSecondaryAxis,
        onDynamicSizeInScrollableAxis: static a => a.sizeInSecondaryAxis
      );
      
      public float sizeInScrollableAxis(bool isHorizontal) => sizeProvider.sizeInScrollableAxis(isHorizontal);

      /// <summary>
      /// Mutable! Will be set to `Some(item's view)` if item is currently visible inside viewport.<br/>
      /// Will be `None` if it's not visible/doesn't support to be visible.
      /// </summary>
      Option<IViewProvider<View>.ViewInstance> visibleInstance;
    
      public virtual void onUpdateLayout(Rect containerSize, Padding padding){}
    
      public bool isVisible => visibleInstance.isSome;
    
      public Option<RectTransform> visibleRt => visibleInstance.mapM(static _ => _.rt);

      protected ElementBase(
        InnerData data, SizeProvider sizeProvider,
        [CanBeNull] IViewProvider<View> maybeViewProvider = default, 
        [Implicit] ILog log = default
      ) {
        this.data = data;
        this.sizeProvider = sizeProvider;
        this.maybeViewProvider = maybeViewProvider?.some() ?? None._;
        tracker = new DisposableTracker(log);
      }

      /// <summary> Will be invoked after <see cref="showOrUpdate"/> creates visual after it was not visible before. </summary>
      protected virtual void becameVisible(View view, RectTransform rt, RectTransform parent) {}
    
      /// <summary> Will be invoked after <see cref="hide"/> was called. </summary>
      protected virtual void becameInvisible() {}

      /// <summary>
      /// Will be called to setup visuals after item became visible inside viewport. It will also be called if
      /// <see cref="data"/> changed and we want to update visuals.
      /// </summary>
      protected abstract void updateState(View view, ITracker tracker);
    
      public Option<RectTransform> showOrUpdate(RectTransform parent, bool forceUpdate) {
        var viewProvider = maybeViewProvider.getOr_RETURN_NONE();
      
        // Create visual if it's not visible yet.
        if (!visibleInstance.valueOut(out var instance)) {
          instance = viewProvider.createItem(parent: parent);
          visibleInstance = Some.a(instance);
          becameVisible(instance.view, rt: instance.rt, parent: parent);
        } 
        // Don't setup visual again be default, because it can be performance intensive.
        else if (!forceUpdate) {
          return Some.a(instance.rt);
        }
        tracker.Dispose();
        updateState(instance.view, tracker);
        return Some.a(instance.rt);
      }

      public void hide() {
        var viewProvider = maybeViewProvider.getOr_RETURN();
        var instance = visibleInstance.getOr_RETURN();
        viewProvider.destroyItem(instance.view);
        visibleInstance = None._;
        tracker.Dispose();
        becameInvisible();
      }
    }
  }
}