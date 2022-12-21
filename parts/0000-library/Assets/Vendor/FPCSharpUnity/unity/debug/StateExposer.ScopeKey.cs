using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  [PublicAPI, Record] public sealed partial class ScopeKey {
    public readonly string name;
    public readonly Option<UnityEngine.Object> unityObject;

    /// <summary>Is this scope still valid?</summary>
    public bool isValid => unityObject.fold(true, obj => obj);

    public static ScopeKey fromString(string name) => new ScopeKey(name, unityObject: None._);
    public static implicit operator ScopeKey(string name) => fromString(name);

    public static ScopeKey fromUnityObject(UnityEngine.Object obj) => new ScopeKey(
      Log.d.isDebug() && obj is GameObject go ? go.transform.debugPath() : obj.name, 
      unityObject: Some.a(obj)
    );
      
    public static implicit operator ScopeKey(UnityEngine.Object obj) => fromUnityObject(obj);
  }
}