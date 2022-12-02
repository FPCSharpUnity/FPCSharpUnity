using UnityEngine;

namespace FPCSharpUnity.unity.gui {
  public sealed class GUIBackgroundColor : GUI.Scope {
    readonly Color value;

    public GUIBackgroundColor(Color color) {
      value = GUI.backgroundColor;
      GUI.backgroundColor = color;
    }

    protected override void CloseScope() {
      GUI.backgroundColor = value;
    }
  }

  public sealed class GUIColor: GUI.Scope {
    readonly Color value;

    public GUIColor(Color color) {
      value = GUI.color;
      GUI.color = color;
    }

    protected override void CloseScope() {
      GUI.color = value;
    }
  }

  public sealed class GUIContentColor : GUI.Scope {
    readonly Color value;

    public GUIContentColor(Color color) {
      value = GUI.contentColor;
      GUI.contentColor = color;
    }

    protected override void CloseScope() {
      GUI.contentColor = value;
    }
  }
}
