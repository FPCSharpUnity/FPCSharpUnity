 using FPCSharpUnity.unity.Data;
 using UnityEditor;
 using UnityEngine;

 namespace FPCSharpUnity.unity.Editor.unity_serialization {
   [CustomPropertyDrawer(typeof(Percentage))]
   public class PercentageDrawer : PropertyDrawer {
     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
       EditorGUI.BeginProperty(position, label, property);
       
       // ReSharper disable once LocalNameCapturedOnly
       var prop = property.FindPropertyRelative("_value");
      
       EditorGUI.BeginChangeCheck();
       var percentage = EditorGUI.Slider(position, label, prop.floatValue * 100, 0, 100);
       EditorGUI.LabelField(position, "%", DrawerUtils.overlayStyle());
       if (EditorGUI.EndChangeCheck()) {
         prop.floatValue = percentage / 100;
       }
       
       EditorGUI.EndProperty();
     }
   }
 }