using System;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  [PublicAPI, Record] public sealed partial class ScopeKey {
    public readonly string name;
    public readonly Option<WeakReference<Object>> unityObject;

    /// <summary>Is this scope still valid?</summary>
    public bool isValid => unityObject.fold(true, objWR => objWR.TryGetTarget(out var obj) && obj);

    public static ScopeKey fromString(string name) => new ScopeKey(name, unityObject: None._);
    public static implicit operator ScopeKey(string name) => fromString(name);

    public static ScopeKey fromUnityObject(Object obj) => new ScopeKey(
      Log.d.isDebug() && obj is GameObject go ? go.transform.debugPath() : obj.name, 
      unityObject: Some.a(obj.weakRef())
    );
      
    public static implicit operator ScopeKey(Object obj) => fromUnityObject(obj);
  }
}