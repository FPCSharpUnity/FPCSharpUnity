using System;
using System.Collections.Generic;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Pools;
using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using UnityEngine;
using AnyExts = FPCSharpUnity.core.exts.AnyExts;

namespace FPCSharpUnity.unity.binding {
  [PublicAPI] public static partial class UnityBind {
    public delegate IDisposable ItemSetupDelegate<in Template, in Data>(Template template, Data data, int index) 
      where Template : Component;
    
    public static ISubscription bind<A>(
      this IRxObservable<A> observable, ITracker tracker, Func<A, ICoroutine> f
    ) {
      var lastCoroutine = F.none<ICoroutine>();
      void stopOpt() { foreach (var c in lastCoroutine) { c.stop(); } }
      var sub = observable.subscribe(
        NoOpDisposableTracker.instance,
        a => {
          stopOpt();
          lastCoroutine = Some.a(f(a));
        }
      ).andThen(stopOpt);
      tracker.track(sub);
      return sub;
    }
    
    public static void bindEnumerable<Template, Data>(
      GameObjectPool<Template> pool,
      IRxObservable<IEnumerable<Data>> rx, 
      ITracker tracker, 
      ItemSetupDelegate<Template, Data> setup,
      bool orderMatters = true,
      Action preUpdate = null,
      Action<List<BindEnumerableEntry<Template>>> afterUpdate = null
    ) where Template : Component {
      var current = new List<BindEnumerableEntry<Template>>();

      rx.subscribe(tracker, list => {
        cleanupCurrent();
        preUpdate?.Invoke();

        var idx = 0;
        foreach (var element in list) {
          var instance = pool.borrow();
          if (orderMatters) instance.transform.SetSiblingIndex(idx);
          var sub = setup(instance, element, idx);
          current.Add(new BindEnumerableEntry<Template>(instance, sub));
          idx++;
        }
        afterUpdate?.Invoke(current);
      });
      // ReSharper disable once PossibleNullReferenceException
      tracker.track(new Subscription(cleanupCurrent));
      
      void cleanupCurrent() {
        foreach (var element in current) {
          if (element.instance) pool.release(element.instance);
          element.subscription.Dispose();
        }
        current.Clear();
      }
    }

    public static IRxVal<ImmutableArrayC<Result>> bindEnumerableRx<Template, Data, Result>(
      GameObjectPool<Template> pool, IRxObservable<IEnumerable<Data>> rx, ITracker tracker, 
      Func<Template, Data, (IDisposable, Result)> setup,
      bool orderMatters = true,
      Action preUpdate = null,
      Action<List<BindEnumerableEntry<Template>>> afterUpdate = null
    ) where Template : Component {
      var resultRx = RxRef.a(ImmutableArrayC<Result>.empty);
      var resultTempList = new List<Result>();
      bindEnumerable(
        pool, rx, tracker: tracker,
        orderMatters: orderMatters,
        preUpdate: () => {
          resultTempList.Clear();
          preUpdate?.Invoke();
        },
        afterUpdate: list => {
          resultRx.value = resultTempList.toImmutableArrayC();
          resultTempList.Clear();
          afterUpdate?.Invoke(list);
        },
        setup: (template, data, index) => {
          var (disposable, result) = setup(template, data);
          resultTempList.Add(result);
          return disposable;
        }
      );
      return resultRx;
    }
    
    public static GameObjectPool<Template> bindEnumerable<Template, Data>(
      string gameObjectPoolName, Template template, IRxObservable<IEnumerable<Data>> rx, ITracker tracker,
      ItemSetupDelegate<Template, Data> setup,
      Action<List<BindEnumerableEntry<Template>>> afterUpdate = null
    ) where Template : Component {
      template.gameObject.SetActive(false);
      var pool = GameObjectPool.a(GameObjectPool.Init.noReparenting(
        gameObjectPoolName,
        create: () => template.clone(parent: template.transform.parent),
        dontDestroyOnLoad: false
      ));
      bindEnumerable(pool, rx, tracker, setup, afterUpdate: afterUpdate);
      return pool;
    }

    [Record] public readonly partial struct BindEnumerableEntry<Template> {
      public readonly Template instance;
      public readonly IDisposable subscription;
    }
  }
}