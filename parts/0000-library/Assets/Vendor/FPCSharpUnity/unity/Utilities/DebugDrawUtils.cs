using UnityEngine;

namespace FPCSharpUnity.unity.Utilities {
  public static class DebugDrawUtils {
    /// <summary>
    /// Draws a ray with specified center point (instead of start point) and a direction vector. 
    /// </summary>
    public static void rayWithMiddlePoint(Vector3 center, Vector3 direction, Color color) => 
      Debug.DrawRay(center - direction / 2f, direction, color);
    
    public static void drawRect(Rect r, Color color, float duration = 0) {
      var a = r.min;
      var b = r.bottomRight();
      var c = r.max;
      var d = r.topLeft();
      Debug.DrawLine(a, b, color, duration);
      Debug.DrawLine(b, c, color, duration);
      Debug.DrawLine(c, d, color, duration);
      Debug.DrawLine(d, a, color, duration);
    }
    
    public static void drawSquare(Vector2 position, float extent, Color color, float duration = 0) {
      var rect = new Rect(position.x - extent, position.y - extent, extent * 2, extent * 2);
      drawRect(rect, color, duration);
    }
    
    public static void drawCircle(Vector2 center, float radius, Color color, float duration = 0) {
      var segments = 32;
      var angle = 0f;
      var prevPos = center + new Vector2(radius, 0);
      for (var i = 0; i < segments; i++) {
        angle += 2 * Mathf.PI / segments;
        var pos = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        Debug.DrawLine(prevPos, pos, color, duration);
        prevPos = pos;
      }
    }
  }
}