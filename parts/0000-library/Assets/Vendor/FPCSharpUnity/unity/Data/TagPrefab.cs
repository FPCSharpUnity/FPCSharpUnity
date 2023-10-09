﻿using System;
using System.Collections.Generic;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Data {
  /// <summary>Mark that A is a prefab.</summary>
  /// You need to extend this class and mark it as <see cref="SerializableAttribute"/>
  /// to serialize it, because Unity does not serialize generic classes.
  [Record, PublicAPI, InlineProperty, Serializable]
  public partial class TagPrefab<A> : TagPrefab, OnObjectValidate where A : Object {
    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
    [SerializeField, NotNull, PublicAccessor, AssetsOnly, HideLabel, GUIColor(167f/255, 183f/255, 211f/255)] 
    A _prefab;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

    #endregion
    
    protected TagPrefab() {}
    
    public bool onObjectValidateIsThreadSafe => false;
    public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) {
#if UNITY_EDITOR
      if (!_prefab) {
        yield return new ErrorMsg($"The {nameof(_prefab)} field is not set.");
        yield break;
      }

      var type = UnityEditor.PrefabUtility.GetPrefabAssetType(_prefab);
      if (type != UnityEditor.PrefabAssetType.Regular && type != UnityEditor.PrefabAssetType.Variant)
        yield return new ErrorMsg($"Expected {_prefab} to be a prefab, but it was {type}!");
#else
      return System.Linq.Enumerable.Empty<ErrorMsg>();
#endif
    }

    public static implicit operator bool(TagPrefab<A> _prefab) => _prefab._prefab;
  }
  
  public abstract class TagPrefab {
    [PublicAPI] public static TagPrefab<A> a<A>(A prefab) where A : Object => new TagPrefab<A>(prefab);
  }

  [PublicAPI]
  public static class TagPrefabExts {
    public static TagPrefab<B> upcastPrefab<A, B>(this TagPrefab<A> aPrefab, B example)
      where A : B 
      where B : Object => 
      TagPrefab.a<B>(aPrefab.prefab);

    public static B instantiate<A, B>(
      this TagPrefab<A> prefab, Transform parent, Func<A, B> mapper
    ) where A : Object {
      var instance = Object.Instantiate(prefab.prefab, parent);
      return mapper(instance);
    }
    
    public static A instantiate<A>(
      this TagPrefab<A> prefab, Transform parent
    ) where A : Object {
      return Object.Instantiate(prefab.prefab, parent);
    }

    public static B instantiate<A, B>(
      this TagPrefab<A> prefab, Vector3 position, Func<A, B> mapper
    ) where A : Object {
      var instance = Object.Instantiate(prefab.prefab, position, Quaternion.identity);
      return mapper(instance);
    }

    public static LazyVal<B> lazyMap<A, B>(
      this TagPrefab<A> prefab, Transform parent, Func<A, B> mapper
    ) where A : Object => Lazy.a(() => instantiate(prefab, parent, mapper));
  }
  
  [Serializable, PublicAPI] public class GameObjectPrefab : TagPrefab<GameObject> { }
  [Serializable, PublicAPI] public class TransformPrefab : TagPrefab<Transform> { }
  [Serializable, PublicAPI] public class RectTransformPrefab : TagPrefab<RectTransform> { }
  [Serializable, PublicAPI] public class ParticleSystemPrefab : TagPrefab<ParticleSystem> { }
}