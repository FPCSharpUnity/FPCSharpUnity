using System;
using System.Diagnostics;
using System.Linq;
using FPCSharpUnity.core.data;
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
    /// <para/>
    /// This should be called BEFORE applying changes to the object.
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
    /// <para/>
    /// This should be called BEFORE applying changes to the object.
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
    /// <para/>
    /// This should be called BEFORE applying changes to the objects.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void recordEditorChangesEditorMode(this Object[] objects, string name) {
#if UNITY_EDITOR
      if (!Application.isPlaying) recordEditorChanges(objects, name);
#endif
    }

    /// <summary>
    /// As <see cref="recordEditorChanges(UnityEngine.Object,string)"/> but for multiple objects.
    /// <para/>
    /// This should be called BEFORE applying changes to the objects.
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
      string title, string body, LogLevel level = LogLevel.INFO, object context = null, ILog log = null
    ) {
      log ??= Log.@default;
      log.mLog(
        level,
        LogEntry.simple(
          $"########## {title} ##########\n\n" +
          $"{body}\n\n" +
          $"############################################################",
          context: context
        )
      );
#if UNITY_EDITOR
      if (!InternalEditorUtility.inBatchMode) EditorUtility.DisplayDialog(title, truncateMessage(body), "OK");
#endif
    }

    /// <summary>
    /// Truncates the message to a specified number of lines.
    /// This is useful when you want to display a message in the dialog, because if the message is too long, it will
    /// hide the dialog buttons.
    /// </summary>
    public static string truncateMessage(
      string message, int maxLines = 50, string appendIfTruncated = "\n... [Full message in logs]"
    ) {
      var lines = message.Split('\n');
      return lines.Length > maxLines 
        ? $"{lines.Take(maxLines).mkString('\n')}{appendIfTruncated}" 
        : message;
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