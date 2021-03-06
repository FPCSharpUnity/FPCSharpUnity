using System;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities {
  public static class RectUtils {
    /** Create rect that has values from percentages of screen. **/
    public static Rect percent(float leftP, float topP, float widthP, float heightP) {
      return new Rect(
        leftP.pWidthToAbs(), topP.pHeightToAbs(),
        widthP.pWidthToAbs(), heightP.pHeightToAbs()
      );
    }

    /** Convert absolute rect to percentage rect. **/
    public static Rect absoluteToPercentage(this Rect pRect) {
      return new Rect(
        pRect.xMin.aWidthToPerc(), pRect.yMin.aHeightToPerc(),
        pRect.width.aWidthToPerc(), pRect.height.aHeightToPerc()
      );
    }

    /** Create rect that has values from percentages of screen. **/
    public static Rect relPercent(float left, float leftEnd, float top, float topEnd) {
      return percent(left, top, leftEnd - left, topEnd - top);
    }

    public static Rect with(
      this Rect rect, float? left = null, float? top = null,
      float? width = null, float? height = null
    ) {
      return new Rect(
        left ?? rect.xMin, top ?? rect.yMin,
        width ?? rect.width, height ?? rect.height
      );
    }

    /* Scale (pivot point: center) */
    public static Rect scale(this Rect rect, float scale) {
      var newW = rect.width * scale;
      var newH = rect.height * scale;
      var wDiff = newW - rect.width;
      var hDiff = newH - rect.height;
      return new Rect(rect.xMin - wDiff / 2, rect.yMin - hDiff / 2, newW, newH);
    }

    public static Rect withMargin(this Rect rect, Vector2 margin)
      => new Rect(rect.min - margin, rect.size + margin * 2);


    /// <summary>
    /// If Option.none is passed Rect is considered to be in world space.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static Rect convertCoordinateSystem(this Rect rect, Option<Transform> from, Transform to) {
      var min = convertPoint(rect.min, from, to);
      var max = convertPoint(rect.max, from, to);
      return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static Vector3 convertPoint(Vector3 localPos, Option<Transform> from, Transform to) {
      var worldPos = from.isSome ? from.__unsafeGet.TransformPoint(localPos) : localPos;
      return to.InverseTransformPoint(worldPos);
    }

    public static Rect encapsulate(this Rect rect, Rect other) => Rect.MinMaxRect(
      Mathf.Min(rect.xMin, other.xMin),
      Mathf.Min(rect.yMin, other.yMin),
      Mathf.Max(rect.xMax, other.xMax),
      Mathf.Max(rect.yMax, other.yMax)
    );

    public static Rect fromCenter(Vector2 center, Vector2 size) =>
      new Rect(new Vector2(center.x -  size.x / 2, center.y -  size.y / 2), size);

    public static float aspectRatio(this Rect rect) => rect.width / rect.height;
    public static Vector2 topLeft(this Rect rect) => new Vector2(rect.min.x, rect.max.y);
    public static Vector2 bottomRight(this Rect rect) => new Vector2(rect.max.x, rect.min.y);

    /// <summary>
    /// Slices the rect and returns right side of it.
    /// </summary>
    public static Rect sliceRight(this Rect rect, float width) {
      rect.xMin = Math.Max(rect.xMin, rect.xMax - width);
      return rect;
    }
    
    public static Rect sliceLeft(this Rect rect, float width) {
      rect.xMax = Math.Min(rect.xMax, rect.xMin + width);
      return rect;
    }
    
    public static Rect sliceTop(this Rect rect, float height) {
      rect.yMax = Math.Min(rect.yMax, rect.yMin + height);
      return rect;
    }
    
    public static Rect sliceBottom(this Rect rect, float height) {
      rect.yMin = Math.Max(rect.yMin, rect.yMax - height);
      return rect;
    }
  }
}
