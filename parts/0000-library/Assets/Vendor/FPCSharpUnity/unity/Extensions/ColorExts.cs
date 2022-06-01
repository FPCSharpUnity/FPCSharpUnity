﻿using System;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class ColorExts {
    public static Color mult(
      this Color color, float r = 1, float g = 1, float b = 1, float a = 1
    ) {
      return new Color(
        color.r * r,
        color.g * g,
        color.b * b,
        color.a * a
      );
    }

    public static Color with(
      this Color color, float r = -1, float g = -1, float b = -1, float a = -1
    ) {
      return new Color(
        r < 0 ? color.r : r,
        g < 0 ? color.g : g,
        b < 0 ? color.b : b,
        a < 0 ? color.a : a
      );
    }

    public static Color multAlpha(this Color color, float alpha) {
      color.a *= alpha;
      return color;
    }

    public static Color withAlpha(this Color color, float alpha) => color.with(a: alpha);

    public static Color32 with32(
      this Color32 color, Option<byte> r = default, Option<byte> g = default, Option<byte> b = default,
      Option<byte> a = default
    ) {
      Option.ensureValue(ref r);
      Option.ensureValue(ref g);
      Option.ensureValue(ref b);
      Option.ensureValue(ref a);
      return new Color32(
        r.getOrElse(color.r),
        g.getOrElse(color.g),
        b.getOrElse(color.b),
        a.getOrElse(color.a)
      );
    }

    public static Color32 with32Alpha(this Color32 color, byte alpha) =>
      color.with32(a: Some.a(alpha));

    public static Color modifyBrightness(this Color rgb, Func<float, float> f) {
      var hsv = rgb.RGBToHSV();
      hsv.b = f(hsv.b);
      return hsv.HSVToRGB();
    }

    public static Color modifySaturation(this Color rgb, Func<float, float> f) {
      var hsv = rgb.RGBToHSV();
      hsv.g = f(hsv.g);
      return hsv.HSVToRGB();
    }

    public static Color RGBToHSV(this Color rgb) {
      if ((double)rgb.b > (double)rgb.g && (double)rgb.b > (double)rgb.r)
        return RGBToHSVHelper(4f, rgb.b, rgb.r, rgb.g);
      else if ((double)rgb.g > (double)rgb.r)
        return RGBToHSVHelper(2f, rgb.g, rgb.b, rgb.r);
      else
        return RGBToHSVHelper(0.0f, rgb.r, rgb.g, rgb.b);
    }

    static Color RGBToHSVHelper(float offset, float dominantcolor, float colorone, float colortwo) {
      var res = new Color();
      res.b = dominantcolor;
      if ((double)res.b != 0.0) {
        float num1 = (double)colorone <= (double)colortwo ? colorone : colortwo;
        float num2 = res.b - num1;
        if ((double)num2 != 0.0) {
          res.g = num2 / res.b;
          res.r = offset + (colorone - colortwo) / num2;
        } else {
          res.g = 0.0f;
          res.r = offset + (colorone - colortwo);
        }
        res.r = res.r / 6f;
        if ((double)res.r >= 0.0)
          return res;
        res.r = res.r + 1f;
      } else {
        res.g = 0.0f;
        res.r = 0.0f;
      }
      return res;
    }

    public static Color HSVToRGB(this Color hsv) {
      Color white = Color.white;
      if ((double)hsv.g == 0.0) {
        white.r = hsv.b;
        white.g = hsv.b;
        white.b = hsv.b;
      } else if ((double)hsv.b == 0.0) {
        white.r = 0.0f;
        white.g = 0.0f;
        white.b = 0.0f;
      } else {
        white.r = 0.0f;
        white.g = 0.0f;
        white.b = 0.0f;
        float num1 = hsv.g;
        float num2 = hsv.b;
        float f = hsv.r * 6f;
        int num3 = (int)Mathf.Floor(f);
        float num4 = f - (float)num3;
        float num5 = num2 * (1f - num1);
        float num6 = num2 * (float)(1.0 - (double)num1 * (double)num4);
        float num7 = num2 * (float)(1.0 - (double)num1 * (1.0 - (double)num4));
        switch (num3 + 1) {
          case 0:
            white.r = num2;
            white.g = num5;
            white.b = num6;
            break;
          case 1:
            white.r = num2;
            white.g = num7;
            white.b = num5;
            break;
          case 2:
            white.r = num6;
            white.g = num2;
            white.b = num5;
            break;
          case 3:
            white.r = num5;
            white.g = num2;
            white.b = num7;
            break;
          case 4:
            white.r = num5;
            white.g = num6;
            white.b = num2;
            break;
          case 5:
            white.r = num7;
            white.g = num5;
            white.b = num2;
            break;
          case 6:
            white.r = num2;
            white.g = num5;
            white.b = num6;
            break;
          case 7:
            white.r = num2;
            white.g = num7;
            white.b = num5;
            break;
        }
        white.r = Mathf.Clamp(white.r, 0.0f, 1f);
        white.g = Mathf.Clamp(white.g, 0.0f, 1f);
        white.b = Mathf.Clamp(white.b, 0.0f, 1f);
      }
      return white;
    }

    public static string toHex(this Color32 color) =>
      color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
  }
}
