﻿using System.Diagnostics;
using FPCSharpUnity.unity.Extensions;
using JetBrains.Annotations;
using FPCSharpUnity.core.reactive;
using UnityEngine;
using UnityEngine.Serialization;

namespace FPCSharpUnity.unity.Components {
  /// <summary>
  /// The only use of this component is to comment some terrible behaviour when you do it from code,
  /// so you would not keep wondering what the heck is happening (e.g. why my properties are changed)
  /// in the editor when inspecting an object.
  /// </summary>
  [PublicAPI] public sealed class CommentComponent : MonoBehaviour {
#if UNITY_EDITOR

    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
    [TextArea, SerializeField, FormerlySerializedAs("comment")] string _comment;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

    #endregion

    public string comment__onlyInEditor {
      get { return _comment; }
      set { _comment = value; }
    }
#endif

    [Conditional("UNITY_EDITOR")] 
    public void setComment(string comment) {
#if UNITY_EDITOR
      _comment = comment;
#endif
    }

    public static ISubscription createGameObject(string name, string comment="") {
      if (Application.isEditor) {
        var go = new GameObject(name);
        go.EnsureComponent<CommentComponent>().setComment(comment);
        DontDestroyOnLoad(go);
        return new Subscription(() => Destroy(go));
      }
      else {
        return Subscription.empty;
      }
    }
  }

  public static class CommentComponentExts {
    [Conditional("UNITY_EDITOR")]
    public static void addCommentComponent(this GameObject go, string comment) {
#if UNITY_EDITOR
      var c = go.EnsureComponent<CommentComponent>();
      c.comment__onlyInEditor =
        string.IsNullOrEmpty(c.comment__onlyInEditor)
        ? comment
        : $"{c.comment__onlyInEditor}\n\n{comment}";
#endif
    }

    [Conditional("UNITY_EDITOR")]
    public static void addCommentComponent(this MonoBehaviour bh, string comment) =>
      bh.gameObject.addCommentComponent(comment);
  }
}