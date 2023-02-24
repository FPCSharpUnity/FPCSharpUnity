using System;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Pools;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout {
  /// <summary> Manages creation and disposal of item's visual. </summary>
  /// <typeparam name="Obj">Visual's type.</typeparam>
  /// 
  // ReSharper disable once TypeParameterCanBeVariant
  // CS8427: Enums, classes, and structures cannot be declared in an interface that has an 'in' or 'out' type parameter.
  public partial interface IViewProvider<Obj> {
    /// <summary> Start showing the item's visuals. </summary>
    /// <param name="parent">Where to place the new visual if it gets instantiated.</param>
    ViewInstance createItem(RectTransform parent);

    /// <summary> Dispose of item's visual. </summary>
    void destroyItem(Obj obj);

    /// <summary> View and it's <see cref="RectTransform"/>. </summary>
    [Record] public readonly partial struct ViewInstance {
      public readonly Obj view;
      public readonly RectTransform rt;
    }
  }

  public static partial class ViewProvider {
    /// <inheritdoc cref="SingleInstance{Obj}"/>
    public static SingleInstance<Obj> singleInstance<Obj>(
      Obj instance, [CanBeNull] Action<bool, Obj> maybeCustomSetActiveCallback = null
    ) where Obj : Component => new(instance: instance, maybeCustomSetActiveCallback: maybeCustomSetActiveCallback);

    public static Pooled<Obj> pooled<Obj>(GameObjectPool<Obj> pool) where Obj : Component => 
      new Pooled<Obj>(pool);

    /// <summary>
    /// Item visual is already placed inside scene/prefab and we reuse a single instance of this item visual.
    /// </summary>
    [Record] public partial class SingleInstance<Obj> : IViewProvider<Obj> where Obj : Component {
      readonly Obj instance;
      readonly Option<Action<bool, Obj>> maybeCustomSetActiveCallback;

      public SingleInstance(Obj instance, [CanBeNull] Action<bool, Obj> maybeCustomSetActiveCallback = null) {
        this.instance = instance;
        this.maybeCustomSetActiveCallback = Option.a(maybeCustomSetActiveCallback);
      }

      public IViewProvider<Obj>.ViewInstance createItem(RectTransform parent) {
        setActive(true);
        return new(instance, (RectTransform) instance.transform);
      }

      public void destroyItem(Obj obj) => setActive(false);

      void setActive(bool flag) {
        if (maybeCustomSetActiveCallback.valueOut(out var customCallback)) customCallback(flag, instance);
        else instance.setActiveGO(flag);
      }
    }

    /// <summary> Items' visuals are created and released by <see cref="GameObjectPool"/>. </summary>
    [Record] public partial class Pooled<Obj> : IViewProvider<Obj> where Obj : Component {
      readonly GameObjectPool<Obj> pool;

      public IViewProvider<Obj>.ViewInstance createItem(RectTransform parent) {
        var v = pool.borrow();
        return new(v, (RectTransform) v.transform);
      }

      public void destroyItem(Obj obj) => pool.release(obj);
    }
  }
}