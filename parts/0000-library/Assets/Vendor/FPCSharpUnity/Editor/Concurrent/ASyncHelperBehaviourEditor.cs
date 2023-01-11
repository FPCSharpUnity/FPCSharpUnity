using FPCSharpUnity.core.pools;
using FPCSharpUnity.core.reactive;
using UnityEditor;
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.Concurrent {
  [CustomEditor(typeof(ASyncHelperBehaviour))]
  public class ASyncHelperBehaviourEditor : UnityEditor.Editor {
    [SerializeField] bool toggleOnUpdate, toggleOnLateUpdate, toggleOnPause, toggleOnQuit;
    
    public override void OnInspectorGUI() {
      var helper = (ASyncHelperBehaviour) serializedObject.targetObject;
      
      renderGroup(nameof(helper.onUpdate), ref toggleOnUpdate, helper.onUpdate);
      renderGroup(nameof(helper.onLateUpdate), ref toggleOnLateUpdate, helper.onLateUpdate);
      renderGroup(nameof(helper.onPause), ref toggleOnPause, helper.onPause);
      renderGroup(nameof(helper.onQuit), ref toggleOnQuit, helper.onQuit);

      void renderGroup(string groupTitle, ref bool toggle, IRxObservable observable) {
        toggle = EditorGUILayout.Foldout(toggle, $"{s(groupTitle)} subscribers: {s(observable.subscriberCount)}");
        if (toggle) {
          using var listDisposable = ListPool<IRxObservableSub>.instance.BorrowDisposable();
          var list = listDisposable.value;
          
          observable.copySubscriptionsTo(list);
          foreach (var sub in list) {
            EditorGUILayout.LabelField(s(sub.subscribedFrom));
          }
        }
      }
    }
  }
}