#if UNITY_EDITOR
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Logger;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui; 

[CustomPropertyDrawer(typeof(ColorAsset))]
class ColorAssetDrawer : PropertyDrawer {
  // Draws a colored box to easily distinguish multiple ColorAssets from each other.
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    if (property == null || label == null) {
      Log.d.error(
        $"Error rendering drawer for {nameof(ColorAsset)} property: {property.echo()}, {label.echo()}, "
        + "while both should be not null!"
      );
      return;
    }
    
    EditorGUI.PropertyField(position, property, label);
    
    var value = (ColorAsset)property.objectReferenceValue;
    if (value) {
      const int OFFSET = 3;
      var c = GUI.color;
      try {
        GUI.color = value.color;
        GUI.Box(new Rect(
          position.x + OFFSET, position.y + OFFSET, position.height - OFFSET - OFFSET, position.height - OFFSET - OFFSET
        ), "");
      }
      finally {
        GUI.color = c;
      }
    }
  }
}
#endif