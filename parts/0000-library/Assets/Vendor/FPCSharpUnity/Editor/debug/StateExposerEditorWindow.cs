using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ExhaustiveMatching;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Logger;
using JetBrains.Annotations;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.debug {
  [HasLogger]
  public partial class StateExposerEditorWindow : EditorWindow, IMB_OnGUI {
    [MenuItem("Tools/Window/State Exposer")]
    public static void OpenWindow() => GetWindow<StateExposerEditorWindow>("State Exposer").Show();
    
    static readonly LazyVal<GUIStyle> 
      multilineTextStyle = Lazy.a(() => new GUIStyle {
        wordWrap = true, alignment = TextAnchor.UpperLeft, richText = false
      }),
      scopeKeyTextStyle = Lazy.a(() => new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold }),
      objectInstanceTextStyle = Lazy.a(() => new GUIStyle { fontStyle = FontStyle.Bold }),
      longLabelTextStyle = Lazy.a(() => new GUIStyle { fontStyle = FontStyle.Bold });

    readonly HashSet<StructuralEquals<ImmutableList<StateExposer.ScopeKey>>> 
      expandObjects = new(), expandInnerScopes = new();

    readonly HashSet<string> expandHeaderValues = new();

    float repaintEverySeconds = 0.1f;
    DateTime lastRepaint = DateTime.MinValue;
    Vector2 scrollViewPosition;

    [UsedImplicitly] void OnInspectorUpdate() {
      var now = DateTime.UtcNow;
      if (now >= lastRepaint.AddSeconds(repaintEverySeconds)) {
        Repaint();
        lastRepaint = now;
      }
    }
    
    public void OnGUI() {
      scrollViewPosition = EditorGUILayout.BeginScrollView(
        scrollViewPosition, alwaysShowHorizontal: false, alwaysShowVertical: false
      );
      try {
        using (new EditorGUILayout.HorizontalScope()) {
          repaintEverySeconds = EditorGUILayout.FloatField("Repaint every (seconds)", repaintEverySeconds);
          if (GUILayout.Button("Run GC")) {
            var previous = GC.GetTotalMemory(forceFullCollection: false);
            GCUtils.runGC();
            var current = GC.GetTotalMemory(forceFullCollection: false);
            log.info(
              $"Garbage collection performed, previous={previous.toBytesReadable()}, current={current.toBytesReadable()}"
            );
          }
        }

        renderScope(StateExposer.instance.rootScope, ImmutableList<StateExposer.ScopeKey>.Empty.structuralEquals());
      }
      finally {
        EditorGUILayout.EndScrollView();
      }

      void renderScope(StateExposer.Scope scope, StructuralEquals<ImmutableList<StateExposer.ScopeKey>> path) {
        var objects = scope.groupedData.ToArray();
        if (objects.nonEmpty() && foldout(expandObjects, $"Objects ({objects.Length})")) {
          using var _ = new EditorGUI.IndentLevelScope();
          renderObjects();
        }

        var scopes = scope.scopes;
        if (scopes.nonEmpty() && foldout(expandInnerScopes, $"Scopes ({scopes.Length})")) {
          using var _ = new EditorGUI.IndentLevelScope();
          renderScopes();
        }

        void renderObjects() {
          foreach (var grouping in objects) {
            var maybeInstance = grouping.Key;
            
            EditorGUILayout.LabelField(
              maybeInstance.fold("Static", obj => $"instance: {obj} ({obj.GetHashCode()})"),
              objectInstanceTextStyle.strict
            );
            foreach (var data in grouping) {
              using var _ = new EditorGUI.IndentLevelScope();
              if (isMultiline(data.value)) {
                renderLabel();
                using (new EditorGUI.IndentLevelScope()) render(data.value);
              }
              else using (new EditorGUILayout.HorizontalScope()) {
                renderLabel();
                EditorGUILayout.Separator();
                render(data.value);
              }

              void renderLabel() => EditorGUILayout.LabelField($"{data.name}:", longLabelTextStyle.strict);

              static bool isMultiline(StateExposer.RenderableValue value) => value switch {
                StateExposer.ActionValue _ => false,
                StateExposer.BoolValue _ => false,
                StateExposer.EnumerableValue enumerable => enumerable.values.Count != 0,
                StateExposer.FloatValue _ => false,
                StateExposer.HeaderValue _ => true,
                StateExposer.KVValue kv => isMultiline(kv.key) || isMultiline(kv.value),
                StateExposer.ObjectValue _ => false,
                StateExposer.StringValue str => 
                  str.value.Contains('\n')
                  // Assume that if we have long lines they will wrap
                  || str.value.Length > 100,
                _ => throw ExhaustiveMatch.Failed(value)
              };
              
              void render(StateExposer.RenderableValue renderableValue) {
                switch (renderableValue) {
                  case StateExposer.ActionValue act:
                    if (GUILayout.Button(act.label)) act.value();
                    break;
                  case StateExposer.BoolValue b:
                    EditorGUILayout.Toggle(b.value);
                    break;
                  case StateExposer.EnumerableValue enumerable: {
                    using var _ = new EditorGUILayout.VerticalScope();
                    var first = true;
                    if (enumerable.showCount) {
                      EditorGUILayout.LabelField($"{enumerable.values.Count} elements", longLabelTextStyle.strict);
                    }

                    foreach (var value in enumerable.values) {
                      if (!first) EditorGUILayout.Separator();
                      render(value);
                      first = false;
                    }

                    break;
                  }
                  case StateExposer.FloatValue flt:
                    EditorGUILayout.FloatField(flt.value, multilineTextStyle.strict);
                    break;
                  case StateExposer.HeaderValue header: {
                    using var _ = new EditorGUILayout.VerticalScope();
                  
                    bool renderBody;
                    {if (header.header.asString.valueOut(out var renderableAsString)) {
                      // We need some sort of stable identifier to store the foldout state and we can't use object
                      // instances for that, thus we use path and value combined as a string.
                      var id = $"{path.collection.mkStringEnum()}: {renderableAsString}";
                      renderBody = foldoutGeneric(expandHeaderValues, id, renderableAsString);
                    } else {
                      render(header.header);
                      renderBody = true;
                    }}
                  
                    if (renderBody) using (new EditorGUI.IndentLevelScope(header.indentBy)) render(header.value);

                    break;
                  }
                  case StateExposer.KVValue kv: {
                    using var _ = new EditorGUILayout.HorizontalScope();
                    render(kv.key);
                    render(kv.value);
                    break;
                  }
                  case StateExposer.ObjectValue obj:
                    EditorGUILayout.ObjectField(obj.value, typeof(Object), allowSceneObjects: true);
                    break;
                  case StateExposer.StringValue str:
                    EditorGUILayout.LabelField(str.value, multilineTextStyle.strict);
                    break;
                  default:
                    throw ExhaustiveMatch.Failed(renderableValue);
                }
              }
            }
          }
        }
        
        void renderScopes() {
          foreach (var (key, innerScope) in scopes.OrderBySafe(_ => _.Key.name)) {
            EditorGUILayout.LabelField(key.name, scopeKeyTextStyle.strict);
            {if (key.unityObject.flatMap(_ => _.Target()).valueOut(out var unityObject)) {
              using var _ = new EditorGUI.IndentLevelScope();
              EditorGUILayout.ObjectField(unityObject, typeof(Object), allowSceneObjects: true);
            }}

            renderScope(innerScope, path.collection.Add(key).structuralEquals());
          }
        }

        bool foldout(ISet<StructuralEquals<ImmutableList<StateExposer.ScopeKey>>> set, string name) => 
          foldoutGeneric(set, path, name);

        // Returns true if we should render the value.
        bool foldoutGeneric<Value>(ISet<Value> set, Value value, string name) {
          var ret = EditorGUILayout.Foldout(set.Contains(value), name);
          if (ret) set.Add(value);
          else set.Remove(value);
          return ret;
        }
      }
    }
  }
}