#if UNITY_EDITOR
using System.Collections.Generic;
using FPCSharpUnity.unity.Utilities;
using FPCSharpUnity.core.exts;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.editor {
  /// <summary>
  /// A simple GUI table drawer.
  /// Usage:
  ///   1. Provide column widths in the constructor.
  ///      Last column will scale down if it is too big.
  ///      It will draw a vertical separator line between each cell.
  ///   2. Call <see cref="beginRow"/> before every table row.
  ///   3. Call <see cref="beginCell"/> before every cell.
  ///   4. Call draw functions for cell contents.
  ///   5. Call <see cref="endTable"/> after all rows and cells are done.
  /// </summary>
  public class EditorGuiTableDrawer {
    readonly int[] columnWidths;

    // Workaround for an issue where we want to use BeginArea after GetRect.
    // http://answers.unity.com/answers/1349110/view.html
    readonly List<Rect> rectsCache = new();

    int currentColumnIdx, linesStarted;
    Rect currentRect;
    bool rowInProgress, areaInProgress;

    public EditorGuiTableDrawer(int[] columnWidths) {
      this.columnWidths = columnWidths;
    }

    public void beginRow(float height = 18) {
      tryEndLine();
      {
        if (linesStarted >= rectsCache.Count) {
          rectsCache.Add(new Rect());
        }
        var temporaryRect = GUILayoutUtility.GetRect(
          minWidth: 0, maxWidth: 10000, minHeight: height, maxHeight: height
        );
        if (Event.current.type == EventType.Repaint) {
          // see comment on rectsCache
          rectsCache[linesStarted] = temporaryRect;
        }
      }

      currentRect = rectsCache[linesStarted];
      rowInProgress = true;
      currentColumnIdx = 0;
      linesStarted++;
    }

    public void beginCell() => beginCells(1);

    /// <param name="cellCount">Combine multiple cells of a single row into one cell.</param>
    public void beginCells(uint cellCount) {
      tryEndArea();
      var width = 0;
      for (var i = 0; i < cellCount; i++) {
        width += columnWidths.getOrElse(currentColumnIdx, 0);
        currentColumnIdx++;
      }

      var rectLeftSide = currentRect.sliceLeft(width);
      currentRect.xMin = rectLeftSide.xMax;
      GUILayout.BeginArea(rectLeftSide, GUIContent.none);
      GUILayout.BeginHorizontal();
      areaInProgress = true;
    }

    public void endTable() {
      tryEndLine();
      linesStarted = 0;
    }

    void tryEndLine() {
      if (!rowInProgress) return;
      tryEndArea();
      while (currentColumnIdx <= columnWidths.Length) {
        beginCell();
        tryEndArea();
      }

      currentColumnIdx = 0;
      rowInProgress = false;
    }

    void tryEndArea() {
      if (areaInProgress) {
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        areaInProgress = false;
        guiVerticalLine();
      }
    }
    
    void guiVerticalLine() {
      const float LINE_WIDTH = 1;
      var rect = currentRect.sliceLeft(LINE_WIDTH);
      const float SPACING = 2;
      currentRect.xMin = rect.xMax + SPACING;
      var prevColor = GUI.color;
      GUI.color = EditorStyles.label.normal.textColor;
      GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
      GUI.color = prevColor;
    }
  }
}
#endif