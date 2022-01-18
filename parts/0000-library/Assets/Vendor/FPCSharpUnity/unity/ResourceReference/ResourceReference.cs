﻿using System;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Filesystem;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.ResourceReference {
  /// <summary>
  /// A thing you save in to resources folder that allows you to reference something outside
  /// of resources folder.
  /// </summary>
  public abstract partial class ResourceReference<A> : ScriptableObject where A : Object {
#pragma warning disable 649
    [SerializeField, NotNull, PublicAccessor] A _reference;
#pragma warning restore 649

#if UNITY_EDITOR
    public A editorReference {
      set => _reference = value;
    }
#endif
  }

  public static class ResourceReference {
#if UNITY_EDITOR
    [PublicAPI]
    public static SO create<SO, A>(string path, A reference)
      where SO : ResourceReference<A> where A : Object
    {
      var so = ScriptableObject.CreateInstance<SO>();
      so.editorReference = reference;
      UnityEditor.AssetDatabase.CreateAsset(so, path);
      return so;
    }
#endif

    static A getReferenceFromResource<A>(ResourceReference<A> resourceReference) where A : Object {
      var reference = resourceReference.reference;
      // When we try to load the resourceReference for the second time, we are getting a cached version 
      // from the memory, not from the disk.
      //
      // This leads to scenarios, where you unload the referenced resource from memory and expect that
      // loading ResourceReference will reload it back to the memory. But because the ResourceReference
      // itself is still in the memory, Unity will happily give it back to you, with the broken reference
      // inside of it.
      //
      // To make sure that the resourceReference and all it's dependencies gets reloaded from disk,
      // we unload resourceReference here. This way, upon the next load, we are sure that it will not have
      // a broken reference inside.
      Resources.UnloadAsset(resourceReference);
      return reference;
    }

    [PublicAPI]
    public static Either<ErrorMsg, A> load<A>(PathStr loadPath) where A : Object => 
      ResourceLoader.load<ResourceReference<A>>(loadPath).mapRight(getReferenceFromResource);

    [PublicAPI]
    public static Tpl<IAsyncOperation, Future<Either<ErrorMsg, A>>> loadAsync<A>(
      PathStr loadPath
    ) where A : Object =>
      ResourceLoader.loadAsync<ResourceReference<A>>(loadPath)
        .map2(future => future.mapT(getReferenceFromResource));

    [PublicAPI]
    public static Tpl<IAsyncOperation, Future<A>> loadAsyncIgnoreErrors<A>(
      PathStr loadPath, [Implicit] ILog log=default, LogLevel logLevel=LogLevel.ERROR
    ) where A : Object =>
      ResourceLoader.loadAsyncIgnoreErrors<ResourceReference<A>>(loadPath, log, logLevel)
        .map2(future => future.map(getReferenceFromResource));
  }
}
