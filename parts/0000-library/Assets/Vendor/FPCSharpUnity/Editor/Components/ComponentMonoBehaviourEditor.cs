using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;

namespace FPCSharpUnity.unity.Components {
  [CustomEditor(typeof(ComponentMonoBehaviour), editorForChildClasses: true)]
  public class ComponentMonoBehaviourEditor : OdinEditor {
    public override void OnInspectorGUI() {
      if (GeneralDrawerConfig.Instance.ShowMonoScriptInEditor) {
        // standalone editor
        GUIHelper.PushGUIEnabled(false);
        base.OnInspectorGUI();
        GUIHelper.PopGUIEnabled();
      }
      else {
        // inline editor
        base.OnInspectorGUI();
      }
    }
  }
}