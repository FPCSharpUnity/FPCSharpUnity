#if UNITY_EDITOR && ODIN_INSPECTOR
using FPCSharpUnity.core.macros;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.unity_serialization; 

public class SerializableUnionDrawer : OdinAttributeDrawer<UnionAttribute> {
  protected override void DrawPropertyLayout(GUIContent label) {
    var caseProp = Property.FindChild(prop => prop.Name == "___case", includeSelf: false);
    caseProp.Draw(label);
    var selectedIdx = (int) caseProp.ValueEntry.WeakValues[0];
    var valueProp = Property.FindChild(prop => prop.Name == $"__{selectedIdx}", includeSelf: false);
    EditorGUI.indentLevel += 1;
    valueProp.Draw(new GUIContent(text: "Union Value"));
    EditorGUI.indentLevel -= 1;
  }
}
#endif