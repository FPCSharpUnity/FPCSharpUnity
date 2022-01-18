using System;
using System.Diagnostics;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#endif

namespace FPCSharpUnity.unity.Utilities {
  public static class EditorUtils {
    /// <summary>
    /// As <see cref="recordEditorChanges(Object,string)"/> but only records the changes if you are not currently in
    /// the play mode.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void recordEditorChangesEditorMode(this Object o, string name) {
#if UNITY_EDITOR
      if (!Application.isPlaying) recordEditorChanges(o, name);
#endif
    }
    
    /// <summary>
    /// Adds the changes that you're going to do to the Unity editor undo stack with the <see cref="name"/>. Works both
    /// in edit and play modes.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void recordEditorChanges(this Object o, string name) {
#if UNITY_EDITOR
      Undo.RecordObject(o, name);
      EditorUtility.SetDirty(o);
#endif
    }

    /// <summary>
    /// As <see cref="recordEditorChangesEditorMode(UnityEngine.Object,string)"/> but for multiple objects. 
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void recordEditorChangesEditorMode(this Object[] objects, string name) {
#if UNITY_EDITOR
      if (!Application.isPlaying) recordEditorChanges(objects, name);
#endif
    }

    /// <summary>
    /// As <see cref="recordEditorChanges(UnityEngine.Object,string)"/> but for multiple objects. 
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void recordEditorChanges(this Object[] objects, string name) {
#if UNITY_EDITOR
      Undo.RecordObjects(objects, name);
      foreach (var o in objects) {
        EditorUtility.SetDirty(o);
      }
#endif
    }

    public static bool inBatchMode =>
#if UNITY_EDITOR
      InternalEditorUtility.inBatchMode
#else
      false
#endif
      ;

    [PublicAPI]
    public static void userInfo(
      string title, string body, LogLevel level = LogLevel.INFO, object context = null
    ) {
      var log = Log.@default;
      if (log.willLog(level)) log.log(
        level,
        LogEntry.simple(
          $"########## {title} ##########\n\n" +
          $"{body}\n\n" +
          $"############################################################",
          context: context
        )
      );
#if UNITY_EDITOR
      const int lineCount = 50;
      var lines = body.Split('\n');
      if (lines.Length > lineCount) body = $"{lines.Take(lineCount).mkString('\n')}\n... [Full message in logs]";
      if (!InternalEditorUtility.inBatchMode) EditorUtility.DisplayDialog(title, body, "OK");
#endif
    }

    [PublicAPI] public static Exception userException(string title, string body, object context = null) {
      userInfo(title, body, LogLevel.ERROR, context);
      return new Exception("Aborting.");
    }

    [PublicAPI] public static Exception userException(string title, ErrorMsg errorMsg) =>
      userException(title, errorMsg.s, errorMsg.context.getOrNull());

#if UNITY_EDITOR
    public enum DisplayDialogResult : byte { OK, Alt, Cancel }
    public static DisplayDialogResult displayDialogComplex(
      string title, string message, string ok, string cancel, string alt
    ) {
      // Alt and cancel is mixed intentionally
      // Unity maps 'x button' and 'keyboard esc' to alt and not to cancel for some reason
      var result = EditorUtility.DisplayDialogComplex(
        title: title, message: message, ok: ok, cancel: alt, alt: cancel
      );
      return result switch {
        0 => DisplayDialogResult.OK,
        1 => DisplayDialogResult.Alt,
        2 => DisplayDialogResult.Cancel,
        _ => throw new ArgumentOutOfRangeException($"Unknown return value from unity: {result}")
      };
    }
#endif
  }
}