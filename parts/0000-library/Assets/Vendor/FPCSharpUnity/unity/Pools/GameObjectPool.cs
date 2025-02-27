#nullable enable

using System;
using System.Collections.Generic;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.core.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Pools {
  public static class GameObjectPool {
    [PublicAPI] public static class Init {
      /// <summary>
      /// Basically an apply method for <see cref="Init{T}.withReparenting"/>. 
      /// </summary>
      public static Init<T> withReparenting<T>(
        string name, Func<T> create,
        SetActive<T>? setActive = null, Action<T>? wakeUp = null, Action<T>? sleep = null,
        bool dontDestroyOnLoad = true, Transform? parent = null
      ) => Init<T>.withReparenting(name, create, setActive, wakeUp: wakeUp, sleep: sleep, dontDestroyOnLoad, parent);

      /// <summary>
      /// Basically an apply method for <see cref="Init{T}.noReparenting"/>. 
      /// </summary>
      public static Init<T> noReparenting<T>(
        string name, Func<T> create, bool dontDestroyOnLoad,
        SetActive<T>? setActive = null, Action<T>? wakeUp = null, Action<T>? sleep = null
      ) => Init<T>.noReparenting(
        name, create, setActive, wakeUp: wakeUp, sleep: sleep, dontDestroyOnLoad
      );
    }

    public delegate void SetActive<in A>(A a, GameObject gameObject, bool active);
    
    public class Init<T> {
      public readonly string name;
      public readonly Func<T> create;
      /// <summary>
      /// If provided, invoked instead of a call to <see cref="GameObject.SetActive"/>.
      /// </summary>
      public readonly SetActive<T>? setActive;
      public readonly Action<T>? wakeUp, sleep;
      public readonly bool dontDestroyOnLoad;

      // Some: parent transform for GameObjectPool. (null = root)
      // None: no reparenting, gameobjects are only disabled on release.
      public readonly Option<Transform?> parent;

      Init(
        string name, Func<T> create, Option<Transform?> parent, SetActive<T>? setActive = null,
        Action<T>? wakeUp = null, Action<T>? sleep = null,
        bool dontDestroyOnLoad = true
      ) {
        this.name = name;
        this.create = create;
        this.setActive = setActive;
        this.wakeUp = wakeUp;
        this.sleep = sleep;
        this.dontDestroyOnLoad = dontDestroyOnLoad;
        this.parent = parent;
      }

      [PublicAPI]
      public static Init<T> withReparenting(
        string name, Func<T> create,
        SetActive<T>? setActive = null, Action<T>? wakeUp = null, Action<T>? sleep = null,
        bool dontDestroyOnLoad = true, Transform? parent = null
      ) => new Init<T>(
        name, create, Some.a(parent), setActive, wakeUp, sleep, dontDestroyOnLoad
      );

      [PublicAPI]
      public static Init<T> noReparenting(
        string name, Func<T> create,
        SetActive<T>? setActive = null, Action<T>? wakeUp = null, Action<T>? sleep = null,
        bool dontDestroyOnLoad = true
      ) => new Init<T>(
        name, create, None._, setActive, wakeUp, sleep, dontDestroyOnLoad
      );
    }

    public static GameObjectPool<T> a<T>(
      Init<T> init, Func<T, GameObject> toGameObject, int initialSize = 0, Option<int> maxSize = default,
      IProfiledScope? maybeProfiledScope = null
    ) => new GameObjectPool<T>(init, toGameObject, initialSize: initialSize, maybeProfiledScope: maybeProfiledScope);

    public static GameObjectPool<GameObject> a(
      Init<GameObject> init, int initialSize = 0, Option<int> maxSize = default,
      IProfiledScope? maybeProfiledScope = null
    ) => new GameObjectPool<GameObject>(
      init, _ => _, initialSize: initialSize, maxSize: maxSize, maybeProfiledScope: maybeProfiledScope
    );

    public static GameObjectPool<A> a<A>(
      Init<A> init, int initialSize = 0, Option<int> maxSize = default,
      IProfiledScope? maybeProfiledScope = null
    ) where A : Component =>
      new GameObjectPool<A>(init, initialSize: initialSize, maxSize: maxSize, maybeProfiledScope: maybeProfiledScope, 
        toGameObject: a => {
          if (!a) Log.d.error(
            $"Component {typeof(A)} is destroyed in {nameof(GameObjectPool)} '{init.name}'!"
          ); 
          return a!.gameObject;
        }
      );
  }

  public class GameObjectPool<T> {
    readonly Stack<T> values;
    readonly Option<Transform> rootOpt;

    readonly Func<T, GameObject> toGameObject;
    readonly Func<T> create;
    readonly GameObjectPool.SetActive<T>? setActive;
    
    /// <summary>Invoked when the object is borrowed out of the pool.</summary>
    readonly Action<T>? wakeUp;
    
    /// <summary>Invoked when the object is released back to the pool.</summary>
    readonly Action<T>? sleep;
    
    readonly bool dontDestroyOnLoad;
    public Option<int> maybeMaxSize;
    
    /// <summary> Profiled scope invoked on <see cref="borrow"/> and <see cref="release"/> operations. </summary>
    readonly IProfiledScope? maybeProfiledScope;

    int instantiatedCount;
    
    [LazyProperty] static ILog log => Log.d.withScope(nameof(GameObjectPool));

    public GameObjectPool(
      GameObjectPool.Init<T> init, Func<T, GameObject> toGameObject, int initialSize = 0,
      Option<int> maxSize = default, IProfiledScope? maybeProfiledScope = null
    ) {
      rootOpt = init.parent.mapM(parent => {
        var rootParent = new GameObject($"{nameof(GameObjectPool)}: {init.name}").transform;
        rootParent.parent = parent;
        if (init.dontDestroyOnLoad) Object.DontDestroyOnLoad(rootParent.gameObject);
        return rootParent;
      });

      this.toGameObject = toGameObject;
      create = init.create;
      setActive = init.setActive;
      wakeUp = init.wakeUp;
      sleep = init.sleep;
      maybeMaxSize = maxSize;
      dontDestroyOnLoad = init.dontDestroyOnLoad;
      this.maybeProfiledScope = maybeProfiledScope;
      var limitedInitialSize = maxSize.foldM(initialSize, maxSizeVal => Math.Min(maxSizeVal, initialSize));
      values = limitedInitialSize == 0 ? new Stack<T>() : new Stack<T>(limitedInitialSize);

      for (var i = 0; i < limitedInitialSize; i++) {
        release(createAndInit());
      }
    }

    /// <summary>
    /// Fill pool up to a specified <see cref="size"/> of elements.
    /// </summary>
    public void initialize(int size) {
      var limitedSize = maybeMaxSize.valueOut(out var maxSize) ? Math.Min(maxSize, size) : size;
      var toInitialize = Math.Max(limitedSize - values.Count, 0);
      
      for (var i = 0; i < toInitialize; i++) {
        release(createAndInit());
      }
    }
    
    public bool instantiateOneIfNotFull(int maxPoolSize) {
      if (instantiatedCount >= maxPoolSize) return true;
      release(createAndInit());
      return false;
    }

    T createAndInit() {
      instantiatedCount++;
      var result = create();
      var go = toGameObject(result);
      var t = go.transform;
      if (dontDestroyOnLoad && !t.parent) Object.DontDestroyOnLoad(go);
      return result;
    }

    /// <summary>Number of items pooled.</summary>
    public int pooledCount => values.Count;

    public T borrow() {
      maybeProfiledScope?.begin();
      
      var result = values.Count > 0 ? values.Pop() : createAndInit();
      var go = toGameObject(result);
      var t = go.transform;
      t.localPosition = Vector3.zero;
      t.rotation = Quaternion.identity;
      
      if (setActive != null) setActive(result, go, true);
      else go.SetActive(true);
      
      wakeUp?.Invoke(result);
      
      maybeProfiledScope?.end();
      return result;
    }

    public void release(
      T value, 
      [Implicit] CallerData callerData = default
    ) {
      try {
        maybeProfiledScope?.begin();
        sleep?.Invoke(value);
        var go = toGameObject(value);
        if (maybeMaxSize.valueOut(out var maxSize) && values.Count >= maxSize) {
          Object.Destroy(go);
        }
        else {
          {if (rootOpt.valueOut(out var root)) {
            go.transform.SetParent(root, false);
          }}

          if (setActive != null) setActive(value, go, false); 
          else go.SetActive(false);
          
          values.Push(value);
        }
      }
      catch (Exception e) {
        // We don't want to spam the log with errors when the game has already been stopped.
        if (!ApplicationUtils.isQuitting) {
          log.error("Could not release object to the pool. You probably unloaded the scene. " + callerData.asShortString(), e);
        }
      }
      finally {
        maybeProfiledScope?.end();
      }
    }

    public void dispose(Action<T> disposeFn) {
      foreach (var value in values) {
        disposeFn(value);
      }

      {if (rootOpt.valueOut(out var root)) {
        Object.Destroy(root.gameObject);
      }}
      
      values.Clear();
    }

    public Disposable<T> BorrowDisposable() => new Disposable<T>(borrow(), _ => release(_));
  }
}