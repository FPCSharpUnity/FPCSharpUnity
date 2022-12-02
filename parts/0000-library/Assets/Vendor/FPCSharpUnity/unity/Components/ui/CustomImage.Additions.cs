using FPCSharpUnity.unity.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Components.ui {
  public partial class CustomImage {
  
    const float referencePPU = 100;
  
    [LabelText("$" + nameof(labelText))]
    [SerializeField] float spritePixelsPerUnit = referencePPU;
  
    string labelText => type == Type.SlicedOutsideFixedBorderSize ? "Border Size" : "Pixels Per Unit";

    bool isSimple => type == Type.Simple;
    bool showPreserveAspect => type is Type.Simple or Type.SimpleOutside or Type.Filled;
    bool showFillCenter => type is Type.Sliced or Type.Tiled;
    bool showFillMethod => type is Type.Filled or Type.TransparentEdgesBoth or Type.TransparentEdgesLeft
      or Type.TransparentEdgesRight;
    bool showFillValues => type is Type.Filled;
  
    public float pixelsPerUnit
    {
      get
      {
        float originalSpritePixelsPerUnit = 100;
        if (activeSprite)
          originalSpritePixelsPerUnit = activeSprite.pixelsPerUnit;

        if (canvas)
          m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                
        var ppuScale = spritePixelsPerUnit / referencePPU;
        return originalSpritePixelsPerUnit / m_CachedReferencePixelsPerUnit * ppuScale;
      }
    }
  
    void GenerateTransparentEdgesSprite(VertexHelper toFill, bool transparentLeft, bool transparentRight) {
      Vector4 v = GetDrawingDimensions(false);
      var transpColorLeft = transparentLeft ? color.withAlpha(0) : color;
      var transpColorRight = transparentRight ? color.withAlpha(0) : color;

      if (m_FillMethod == FillMethod.Horizontal) {
        var wd = (v.z - v.x) * fillAmount;
        toFill.Clear();

        AddQuad(
          toFill,
          new Vector2(v.x, v.y),
          new Vector2(v.x + wd, v.w),
          transpColorLeft, transpColorLeft, color, color,
          new Vector2(0, 0),
          new Vector2(fillAmount, 1)
        );
        AddQuad(
          toFill,
          new Vector2(v.x + wd, v.y),
          new Vector2(v.z - wd, v.w),
          color, color, color, color,
          new Vector2(fillAmount, 0),
          new Vector2(1 - fillAmount, 1)
        );
        AddQuad(
          toFill,
          new Vector2(v.z - wd, v.y),
          new Vector2(v.z, v.w),
          color, color, transpColorRight, transpColorRight,
          new Vector2(1 - fillAmount, 0),
          new Vector2(1, 1)
        );
      }

      if (m_FillMethod == FillMethod.Vertical) {
        var wd = (v.w - v.y) * fillAmount;
        toFill.Clear();

        AddQuad(
          toFill,
          new Vector2(v.x, v.y),
          new Vector2(v.z, v.y + wd),
          transpColorLeft, color, color, transpColorLeft,
          new Vector2(0, 0),
          new Vector2(1, fillAmount)
        );
        AddQuad(
          toFill,
          new Vector2(v.x, v.y + wd),
          new Vector2(v.z, v.w - wd),
          color, color, color, color,
          new Vector2(0, fillAmount),
          new Vector2(1, 1 - fillAmount)
        );
        AddQuad(
          toFill,
          new Vector2(v.x, v.w - wd),
          new Vector2(v.z, v.w),
          color, transpColorRight, transpColorRight, color,
          new Vector2(0, 1 - fillAmount),
          new Vector2(1, 1)
        );
      }
    }
  
    private void GenerateSlicedSpriteV2(VertexHelper toFill, bool fixedBorderSize) {
      if (!hasBorder) {
        GenerateSimpleSprite(toFill, false);
        return;
      }

      Vector4 outer, inner, padding, border;

      if (activeSprite != null) {
        outer = DataUtility.GetOuterUV(activeSprite);
        inner = DataUtility.GetInnerUV(activeSprite);
        padding = DataUtility.GetPadding(activeSprite);
        border = activeSprite.border;
      }
      else {
        outer = Vector4.zero;
        inner = Vector4.zero;
        padding = Vector4.zero;
        border = Vector4.zero;
      }

      Rect rect = GetPixelAdjustedRect();
      padding = padding / pixelsPerUnit;

      s_VertScratch[1] = Vector2.zero;
      s_VertScratch[2] = rect.size;

      if (fixedBorderSize) {
        var max = Mathf.Max(Mathf.Max(border.x, border.y), Mathf.Max(border.z, border.w));
        for (var i = 0; i < 4; i++) {
          border[i] = border[i] / max * spritePixelsPerUnit;
        }
      }
      else {
        border /= pixelsPerUnit;
      }

      s_VertScratch[0] = s_VertScratch[1] - new Vector2(border.x, border.y);
      s_VertScratch[3] = s_VertScratch[2] + new Vector2(border.z, border.w);

      for (int i = 0; i < 4; ++i) {
        s_VertScratch[i].x += rect.x;
        s_VertScratch[i].y += rect.y;
      }

      s_UVScratch[0] = new Vector2(outer.x, outer.y);
      s_UVScratch[1] = new Vector2(inner.x, inner.y);
      s_UVScratch[2] = new Vector2(inner.z, inner.w);
      s_UVScratch[3] = new Vector2(outer.z, outer.w);

      toFill.Clear();

      for (int x = 0; x < 3; ++x) {
        int x2 = x + 1;

        for (int y = 0; y < 3; ++y) {
          if (!m_FillCenter && x == 1 && y == 1)
            continue;

          int y2 = y + 1;

          AddQuad(toFill,
            new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
            new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
            color,
            new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
            new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
        }
      }
    }
  
    void GenerateSimpleCroppedSprite(VertexHelper vh) {
      Vector4 v = GetDrawingDimensions(false);
      var uv = (activeSprite != null) ? DataUtility.GetOuterUV(activeSprite) : Vector4.zero;

      {
        var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);
        Rect r = GetPixelAdjustedRect();

        if (size.sqrMagnitude > 0.0f) {
          var spriteRatio = size.x / size.y;
          var rectRatio = r.width / r.height;

          if (spriteRatio < rectRatio) {
            var oldHeight = uv.w - uv.y;
            var diff = oldHeight / 2 * (1 - spriteRatio / rectRatio);
            uv.y += diff;
            uv.w -= diff;
          }
          else {
            var oldHeight = uv.z - uv.x;
            var diff = oldHeight / 2 * (1 - rectRatio / spriteRatio);
            uv.x += diff;
            uv.z -= diff;
          }
        }
      }

      var color32 = color;
      vh.Clear();
      vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
      vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
      vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
      vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

      vh.AddTriangle(0, 1, 2);
      vh.AddTriangle(2, 3, 0);
    }
  
    void GenerateSimpleOutsideSprite(VertexHelper vh, bool lPreserveAspect) {
      var v = getDrawingDimensions(lPreserveAspect);
      var uv = (activeSprite != null) ? DataUtility.GetOuterUV(activeSprite) : Vector4.zero;

      vh.Clear();
      var color32 = color;
      vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
      vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
      vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
      vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));
      vh.AddTriangle(0, 1, 2);
      vh.AddTriangle(2, 3, 0);

      /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
      Vector4 getDrawingDimensions(bool shouldPreserveAspect) {
        var padding = activeSprite == null ? Vector4.zero : DataUtility.GetPadding(activeSprite);

        var border = activeSprite == null ? Vector4.zero : activeSprite.border;
        var size =
          activeSprite == null
            ? Vector2.zero
            : new Vector2(
              activeSprite.rect.width - (border.x + border.z), activeSprite.rect.height - (border.y + border.w)
            );

        var sizeFullSprite =
          activeSprite == null
            ? Vector2.zero
            : activeSprite.rect.size;

        var r = rectTransform.rect;


        if (shouldPreserveAspect && size.sqrMagnitude > 0.0f) {
          var spriteRatio = size.x / size.y;
          var rectRatio = r.width / r.height;

          if (spriteRatio > rectRatio) {
            var oldHeight = r.height;
            r.height = r.width * (1.0f / spriteRatio);
            r.y += (oldHeight - r.height) * rectTransform.pivot.y;
          }
          else {
            var oldWidth = r.width;
            r.width = r.height * spriteRatio;
            r.x += (oldWidth - r.width) * rectTransform.pivot.x;
          }
        }

        var borderX = r.width / size.x * border.x;
        var borderY = r.height / size.y * border.y;
        var borderZ = r.width / size.x * border.z;
        var borderW = r.height / size.y * border.w;

        r = Rect.MinMaxRect(
          xmin: r.xMin - borderX,
          ymin: r.yMin - borderY,
          xmax: r.xMax + borderZ,
          ymax: r.yMax + borderW
        );

        var spriteW = Mathf.RoundToInt(sizeFullSprite.x);
        var spriteH = Mathf.RoundToInt(sizeFullSprite.y);

        var v = new Vector4(
          padding.x / spriteW,
          padding.y / spriteH,
          (spriteW - padding.z) / spriteW,
          (spriteH - padding.w) / spriteH);

        return new Vector4(
          r.x + r.width * v.x,
          r.y + r.height * v.y,
          r.x + r.width * v.z,
          r.y + r.height * v.w
        );
      }
    }

    static void AddQuad(
      VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color1, Color32 color2, Color32 color3,
      Color32 color4, Vector2 uvMin, Vector2 uvMax
    ) {
      int startIndex = vertexHelper.currentVertCount;

      vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color1, new Vector2(uvMin.x, uvMin.y));
      vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color2, new Vector2(uvMin.x, uvMax.y));
      vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color3, new Vector2(uvMax.x, uvMax.y));
      vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color4, new Vector2(uvMax.x, uvMin.y));

      vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
      vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
  }
}