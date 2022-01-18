﻿using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.gui {
  public static class EditorGUI_ {
    /// <summary>Drop down that returns index of selected item.</summary>
    public static Option<int> IndexPopup(
      Rect position, Option<int> selectedIdx, string[] displayedOptions
    ) {
      const int NOT_SELECTED = -1;
      return
        EditorGUI.Popup(position, selectedIdx.getOrElse(NOT_SELECTED), displayedOptions)
        .mapVal(value => (value != NOT_SELECTED).opt(value));
    }
  }
}