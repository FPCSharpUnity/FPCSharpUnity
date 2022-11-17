using System;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
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
    /// <inheritdoc cref="Init{TData,TView}.containerSizeInScrollableAxis"/>
    public readonly float containerSizeInScrollableAxis;
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
 
  
  
  public interface IElement {
    ElementBase2.ISizeProvider sizeProvider { get; }
    Option<RectTransform> show(RectTransform parent, bool force);
    void hide();
    void onUpdateLayout(Rect containerSize, Padding padding);
    abstract bool isVisible { get; }
    abstract Option<RectTransform> visibleRt { get; }
    Percentage sizeInSecondaryAxis { get; }
    float sizeInScrollableAxis(bool isHorizontal);
  }
  
  public abstract partial class ElementBase2 : IElement {
    public abstract ISizeProvider sizeProvider { get; }
    public abstract Option<RectTransform> show(RectTransform parent, bool force);
    public abstract void hide();
    public virtual void onUpdateLayout(Rect containerSize, Padding padding){}
    public abstract bool isVisible { get; }
    public abstract Option<RectTransform> visibleRt { get; }
    
    public Percentage sizeInSecondaryAxis => sizeProvider.sizeInSecondaryAxis;
    public float sizeInScrollableAxis(bool isHorizontal) => sizeProvider.sizeInScrollableAxis(isHorizontal);

    public interface ISizeProvider {
      float sizeInScrollableAxis(bool isHorizontal);
      Percentage sizeInSecondaryAxis { get; }
    }
    public static class SizeProvider {
      public partial class FromTemplateStatic : ISizeProvider {
        readonly Rect rect;
        public Percentage sizeInSecondaryAxis { get; }

        public FromTemplateStatic(Component o, Percentage sizeInSecondaryAxis) {
          this.sizeInSecondaryAxis = sizeInSecondaryAxis;
          rect = ((RectTransform)o.transform).rect;
        }

        public static FromTemplateStatic fullViewport(Component o) => new(o, new Percentage(1));

        float ISizeProvider.sizeInScrollableAxis(bool isHorizontal) =>
          isHorizontal ? rect.width : rect.height;

      }
      public partial class Static : ISizeProvider {
        readonly float sizeInScrollableAxis;
        public Percentage sizeInSecondaryAxis { get; }

        public Static(float sizeInScrollableAxis, Percentage sizeInSecondaryAxis) {
          this.sizeInScrollableAxis = sizeInScrollableAxis;
          this.sizeInSecondaryAxis = sizeInSecondaryAxis;
        }

        [LazyProperty]
        public static Memo<(float, Percentage), Static> cached => Memo.a<(float, Percentage), Static>(t =>
          new Static(sizeInSecondaryAxis: t.Item2, sizeInScrollableAxis: t.Item1)
        );

        float ISizeProvider.sizeInScrollableAxis(bool isHorizontal) => sizeInScrollableAxis;
      }

      public class DynamicSizeInScrollableAxis : ISizeProvider {
        public readonly Val<float> sizeInScrollableAxisVal;
        public Percentage sizeInSecondaryAxis { get; }
        
        public DynamicSizeInScrollableAxis(Percentage sizeInSecondaryAxis, Val<float> sizeInScrollableAxis) {
          this.sizeInSecondaryAxis = sizeInSecondaryAxis;
          sizeInScrollableAxisVal = sizeInScrollableAxis;
        }

        public float sizeInScrollableAxis(bool isHorizontal) => sizeInScrollableAxisVal.value;
      }
    }

    public partial interface IViewProvider<Obj> {
      ViewInstance createItem(RectTransform parent);
      void destroyItem(Obj obj);
      
      [Record] public readonly partial struct ViewInstance {
        public readonly Obj view;
        public readonly RectTransform rt;
      }
    }
    public static partial class ViewProvider {
      public class SingleInstance<Obj> : IViewProvider<Obj> where Obj : Component {
        readonly Obj instance;

        public SingleInstance(Obj instance) => this.instance = instance;
        public IViewProvider<Obj>.ViewInstance createItem(RectTransform parent) => new(instance, (RectTransform)instance.transform);
        public void destroyItem(Obj obj) {}
      }

      public static class Pooled {
        public static Pooled<Obj> cached<Obj>(GameObjectPool<Obj> pool) where Obj : Component =>
          Pooled<Obj>.cached[pool];
      }
      public class Pooled<Obj> : IViewProvider<Obj> where Obj : Component {
        readonly GameObjectPool<Obj> pool;

        public Pooled(GameObjectPool<Obj> pool) {
          this.pool = pool;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void reset() => cached.clear();
        
        [LazyProperty] public static Memo<GameObjectPool<Obj>, Pooled<Obj>> cached => Memo.a<GameObjectPool<Obj>, Pooled<Obj>>(t => new(t));

        public IViewProvider<Obj>.ViewInstance createItem(RectTransform parent) {
          var v = pool.borrow();
          return new(v, (RectTransform)v.transform);
        }
        public void destroyItem(Obj obj) => pool.release(obj);
      }
      public class InstantiateAndDestroyEditor<Obj> : IViewProvider<Obj> where Obj : Component {
        readonly Obj template;

        public InstantiateAndDestroyEditor(Obj template) => this.template = template;

        public IViewProvider<Obj>.ViewInstance createItem(RectTransform parent) {
          var v = template.clone(parent: parent);
          return new(v, (RectTransform)v.transform);
        }
        public void destroyItem(Obj obj) => DestroyImmediate(obj.gameObject);
      }
    }
  }
  
  public abstract class ElementBaseWithSetupDelegate<InnerData, View> : ElementBase<InnerData, View> {
    readonly Action<InnerData, View, ITracker> updateStateDelegate;

    protected ElementBaseWithSetupDelegate(
      InnerData data, ISizeProvider sizeProvider, 
      Action<InnerData, View, ITracker> updateStateDelegate,
      [CanBeNull] IViewProvider<View> maybeViewProvider = default, ILog log = default
    ) : base(data, sizeProvider, maybeViewProvider, log) {
      this.updateStateDelegate = updateStateDelegate;
    }

    protected override void updateState(View view, ITracker tracker) => updateStateDelegate(data, view, tracker);
  }
  
  public abstract class ElementBaseForRectTransform : ElementBase<Unit, RectTransform> {
    protected ElementBaseForRectTransform(
      RectTransform rectTransform, Percentage? noneHundredSizeInSecondaryAxis = default, 
      [Implicit] ILog log = default
    ) : base(
      Unit._, 
      sizeProvider: new SizeProvider.FromTemplateStatic(
        rectTransform, sizeInSecondaryAxis: noneHundredSizeInSecondaryAxis ?? new Percentage(1)
      ), maybeViewProvider: new ViewProvider.SingleInstance<RectTransform>(rectTransform), log) {}

    protected override void updateState(RectTransform view, ITracker tracker){}
  }
  
  public abstract class ElementBaseForSpacer : ElementBase<Unit, Unit> {
    protected ElementBaseForSpacer(
      ISizeProvider sizeProvider,
      [Implicit] ILog log = default
    ) : base(Unit._, sizeProvider: sizeProvider, maybeViewProvider: null, log) {}

    protected override void updateState(Unit view, ITracker tracker){}
  }
  
  public abstract class ElementBaseForPooledRow<InnerData, View> : ElementBase<InnerData, View> where View : Component {
    protected ElementBaseForPooledRow(
      InnerData data, View template, GameObjectPool<View> pool,
      [Implicit] ILog log = default
    ) : base(
      data, sizeProvider: SizeProvider.FromTemplateStatic.fullViewport(template), 
      maybeViewProvider: ViewProvider.Pooled.cached(pool), log
    ) {}
  }

  public abstract class ElementBase<InnerData, View> : ElementBase2 {
    readonly IDisposableTracker tracker;
    public InnerData data { get; }
    protected Option<IViewProvider<View>> maybeViewProvider;
    public sealed override ISizeProvider sizeProvider { get; }

    Option<IViewProvider<View>.ViewInstance> visibleInstance { get; set; }

    public sealed override bool isVisible => visibleInstance.isSome;
    public sealed override Option<RectTransform> visibleRt => visibleInstance.map(static _ => _.rt);

    protected ElementBase(
      InnerData data, 
      ISizeProvider sizeProvider,
      [CanBeNull] IViewProvider<View> maybeViewProvider = default, 
      [Implicit] ILog log = default
    ) {
      this.data = data;
      this.sizeProvider = sizeProvider;
      this.maybeViewProvider = maybeViewProvider?.some() ?? None._;
      tracker = new DisposableTracker(log);
    }

    protected virtual void afterCreation(View view, RectTransform rt, RectTransform parent) {}
    
    protected virtual void afterDeletion() {}

    protected abstract void updateState(View view, ITracker tracker);
    
    public sealed override Option<RectTransform> show(RectTransform parent, bool force) {
      var viewProvider = maybeViewProvider.getOr_RETURN_NONE();
      if (!visibleInstance.valueOut(out var instance)) {
        instance = viewProvider.createItem(parent: parent);
        visibleInstance = Some.a(instance);
        afterCreation(instance.view, rt: instance.rt, parent: parent);
      } else if(!force) return Some.a(instance.rt);
      tracker.Dispose();
      updateState(instance.view, tracker);
      return Some.a(instance.rt);
    }

    public sealed override void hide() {
      var viewProvider = maybeViewProvider.getOr_RETURN();
      var instance = visibleInstance.getOr_RETURN();
      viewProvider.destroyItem(instance.view);
      visibleInstance = None._;
      tracker.Dispose();
      afterDeletion();
    }
  }
}