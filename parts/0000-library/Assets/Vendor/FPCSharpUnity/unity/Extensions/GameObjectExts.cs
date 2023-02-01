﻿using System;
using System.Collections.Generic;
using FPCSharpUnity.unity.Components.Forwarders;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.reactive;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI]
  public static class GameObjectExts {
    public static void changeAlpha(this GameObject go, float alpha) {
      foreach (var childRenderer in go.GetComponentsInChildren<Renderer>()) {
        var material = childRenderer.material;
        var c = material.color;
        material.color = new Color(c.r, c.g, c.b, alpha);
      }
    }

    public static void doRecursively(this GameObject go, Action<GameObject> act) {
      act(go);
      go.transform.doRecursively(t => act(t.gameObject));
    }

    public static void setLayerRecursively(this GameObject go, int layer) {
      void setLayer(Transform t) {
        t.gameObject.layer = layer;
        var childCount = t.childCount;
        for (var idx = 0; idx < childCount; idx++) {
          setLayer(t.GetChild(idx));
        }
      }
      
      setLayer(go.transform);
    }

    [PublicAPI]
    public static void replaceWith(this GameObject go, GameObject replacement) {
      var gt = go.transform;
      var rt = replacement.transform;
      var siblingIndex = gt.GetSiblingIndex();
      rt.parent = gt.parent;
      rt.position = gt.position;
      rt.rotation = gt.rotation;
      rt.localScale = gt.localScale;
      Object.Destroy(go);
      rt.SetSiblingIndex(siblingIndex);
    }

    public static ICoroutine everyFrame(this GameObject go, Func<bool> f) => ASync.EveryFrame(go, f);

    public static ICoroutine everyFrame(this GameObject go, Action a) =>
      go.everyFrame(() => { a(); return true; });

    public static IRxObservable<Unit> onMouseDown(this GameObject go) =>
      go.onEvent<Unit, OnMouseDownForwarder>();

    public static IRxObservable<Unit> onMouseUp(this GameObject go) =>
      go.onEvent<Unit, OnMouseUpForwarder>();

    public static IRxObservable<A> onEvent<A, Forwarder>(this GameObject go) where Forwarder : EventForwarder<A> =>
      go.EnsureComponent<Forwarder>().onEvent;

    /// <summary>
    /// Ensures a component exists on a <see cref="GameObject"/> by getting it and adding it if it's not there. Does
    /// not support undoing, if you want that you want to use <see cref="EnsureComponentWithUndo{A}"/>.
    /// </summary>
    public static A EnsureComponent<A>(this GameObject go) where A : Component => 
      go.TryGetComponent<A>(out var comp) ? comp : go.AddComponent<A>();

    /// <summary>
    /// As <see cref="EnsureComponent{A}"/> but support Unity undo system. Works even not in editor.
    /// </summary>
    public static A EnsureComponentWithUndo<A>(this GameObject go) where A : Component {
#if UNITY_EDITOR
      // Well, great. According to https://docs.unity3d.com/ScriptReference/Undo.RecordObject.html
      // `Undo.RecordObject` doesn't track adding components, so we have to use a separate, special API for that. Yay! 
      return go.TryGetComponent<A>(out var comp) ? comp : UnityEditor.Undo.AddComponent<A>(go);
#else
      return go.EnsureComponent<A>();
#endif
    }

    public static Option<A> GetComponentOption<A>(this GameObject go) where A : Component => 
      go.TryGetComponent<A>(out var comp) ? Some.a(comp) : None._;

    public static Either<ErrorMsg, A> GetComponentSafeE<A>(this GameObject go) where A : Component {
      var res = go.GetComponentOption<A>();
      return
        res.isNone
        ? (Either<ErrorMsg, A>) new ErrorMsg($"Can't find component {typeof(A)} on '{go}'")
        : res.__unsafeGet;
    }

    public static string nameOrNull(this GameObject go) =>
      go ? go.name : "none";

    // Modified from unity decompiled dll.
    // Added includeInactive parameter.
    public static Option<T> getComponentInChildren<T>(
      this GameObject go, bool includeInactive
    ) where T : Component {
      if (includeInactive || go.activeInHierarchy) {
        var component = go.GetComponent<T>();
        if (component != null)
          return Some.a(component);
      }
      var transform = go.transform;
      if (transform != null) {
        foreach (Component component in transform) {
          var componentInChildren = component.gameObject.getComponentInChildren<T>(includeInactive);
          if (componentInChildren.isSome)
            return componentInChildren;
        }
      }
      return F.none<T>();
    }

    public static Option<A> getComponentInParents<A>(
      this GameObject go, bool includeSelf = true, [CanBeNull] Func<A, bool> predicate = null
    ) where A : Component {
      Transform current;
      if (includeSelf) {
        current = go.transform;
      } else {
        var parent = go.transform.parent;
        if (parent) current = parent;
        else return None._;
      }
      while (true) {
        var component = current.TryGetComponent<A>(out var comp) ? comp : null;
        if (component && (predicate == null || predicate(component))) return Some.a(component);
        var parent = current.parent;
        if (parent) current = parent;
        else return F.none<A>();
      }
    }

    public static void SetActive(this IList<GameObject> gameObjects, bool active) {
      foreach (var obj in gameObjects) {
        obj.SetActive(active);
      }
    }

    /// <inheritdoc cref="UnityObjectExts.destroySafe"/>
    public static void destroyGameObject(this GameObject go) => go.destroySafe();
  }
}
