using UnityEngine.UI;

namespace FPCSharpUnity.unity.Extensions {
  public static class GraphicExts {
    public static void applyAlpha(this Graphic graphic, float alpha) {
      graphic.color = graphic.color.withAlpha(alpha);
    }
  }
}
