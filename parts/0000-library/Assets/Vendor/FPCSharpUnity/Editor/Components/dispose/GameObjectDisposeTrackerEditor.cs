using UnityEditor;

namespace FPCSharpUnity.unity.Components.dispose {
  [CustomEditor(typeof(GameObjectDisposeTracker))]
  public class GameObjectDisposeTrackerEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {
      var dt = (GameObjectDisposeTracker) serializedObject.targetObject;
      EditorGUILayout.LabelField("Tracked objects:", dt.trackedCount.ToString());
      EditorGUILayout.Space();

      foreach (var t in dt.trackedDisposables) {
        EditorGUILayout.LabelField(t.asString());
      }
    }
  }
}