using System;
using System.Collections.Generic;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.editor;
using FPCSharpUnity.unity.ResourceReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.resources {
  /// <summary>
  /// Allows you to have a reference to Resources folder and have it validated by an object validator.
  /// </summary>
  [PublicAPI, Serializable] public partial class ReferenceToResource<A> : OnObjectValidate where A : Object {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [
      SerializeField, NotNull, PublicAccessor, 
      ValidateInput(nameof(validate), "Can't find the specified name in Resources!")
    ] string _name;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649
    
    public bool onObjectValidateIsThreadSafe => false;
    
    public IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent) {
      if (maybeLoad(_name).leftValueOut(out var error)) {
        yield return new ErrorMsg(error);
      }
    }

    static bool validate(string name) => maybeLoad(name).isRight;

    public A load() => load(_name);
    
    public static A load(string name) =>
      maybeLoad(name)
        .mapLeftM(err => $"{s(err)} This should never happen as the build validation should have caught this!")
        .rightOrThrow;

    public static Either<string, A> maybeLoad(string name) {
#if UNITY_EDITOR
      if (!ResourceLoadHelper.domainLoadedFuture.isCompleted) {
        var message =
          $"Can't load {typeof(A).FullName} from path '{name}' in Resources because the domain is not loaded yet!\n"
          + $"Use `ResourceLoadHelper.domainLoadedFuture` to wait for domain load.";
        // Log message separately so we could explore call stack easily.
        Debug.LogError(message);
        return message;
      }
#endif
      var maybeA = Resources.Load<A>(name);
      return maybeA ? maybeA : $"Can't load {typeof(A).FullName} from path '{name}' in Resources!";
    }
  }
  [Serializable] public sealed class ReferenceToResourceImage : ReferenceToResource<Image> {}
}