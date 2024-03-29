﻿using System;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.core.functional;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FPCSharpUnity.unity.Extensions {
  public static class UnityObjectExts {
#if UNITY_EDITOR
    public static PathStr editorAssetPath(this Object obj) => new PathStr(AssetDatabase.GetAssetPath(obj));
#endif

    public static A dontDestroyOnLoad<A>(this A a) where A : Object {
      Object.DontDestroyOnLoad(a);
      return a;
    }
    
    /// <summary>
    /// Uses <see cref="Object.Destroy(UnityEngine.Object)"/> or <see cref="Object.DestroyImmediate(UnityEngine.Object)"/>
    /// depending on whether we are in play mode or not.
    /// </summary>
    public static void destroySafe(this Object obj) {
      if (!obj) return;
      
      if (Application.isPlaying) Object.Destroy(obj);
      else Object.DestroyImmediate(obj);
    }

    /** Invoke `f` on `a` if it is not dead. */
    public static B optInvoke<A, B>(this A a, Func<A, B> f)
      where A : Object
      where B : Object
    => a ? f(a) : null;

    public static A assertIsSet<A>(this A obj, string name) where A : Object {
      if (!obj) throw new IllegalStateException($"{name} is not set to an object!");
      return obj;
    }
    
    public static Option<GameObject> getGameObject(this Object obj) =>
      obj switch {
        GameObject go => Some.a(go),
        Component c => Some.a(c.gameObject),
        _ => None._
      };
  }
}
