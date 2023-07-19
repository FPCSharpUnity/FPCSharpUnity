using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Tween.fun_tween.serialization.manager;
using GenerationAttributes;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Editor.VisualTweenTimeline {
  [Record] public partial class ElementSelectorResult {
    public readonly Type type;
    public readonly FieldInfo typeField;
    public readonly Object candidate;

    public ISerializedTweenTimelineElementBase createElement() {
      var newElement = (ISerializedTweenTimelineElementBase) Activator.CreateInstance(type);
      typeField.SetValue(newElement, candidate);
      return newElement;
    }
  }

  /// <summary>
  /// Show selector popup for timeline elements filtered for specific object.
  /// Used in Drag & Drop action in Timeline editor.
  /// </summary>
  public class ElementSelector : OdinSelector<ElementSelectorResult> {
    [LazyProperty] public static Type[] allElementTypes => 
      TypeCache.GetTypesDerivedFrom<ISerializedTweenTimelineElementBase>()
      .Where(_ => !_.IsAbstract)
      .ToArray();
    
    readonly ElementSelectorResult[] source;
    readonly bool multipleTargets;

    public ElementSelector(Object targetObject) {
      var typesWithTargetFields = allElementTypes.collect(type => {
        var allAssignableFields = type.getAllFields()
          .Where(field => field.isSerializable() && typeof(Object).IsAssignableFrom(field.FieldType))
          .ToArray();

        // TODO: we can make a more typesafe way to select a required field
        var selectedField = allAssignableFields.find(_ => _.Name == "_target") || allAssignableFields.headOption();

        return selectedField.mapM(_ => (type, field: _));
      }).ToArray();

      var possibleTargetObjects = new List<Object> { targetObject };
      {if (targetObject is GameObject go) {
        possibleTargetObjects.AddRange(go.GetComponents<Component>());
      }}

      source = possibleTargetObjects.SelectMany(targetObject =>
        typesWithTargetFields.collect(tpl => {
          if (tpl.field.FieldType.IsInstanceOfType(targetObject)) {
            return Some.a(new ElementSelectorResult(tpl.type, tpl.field, targetObject));
          }
          return None._;
        }).OrderBySafe(_ => _.type.Name)
      ).ToArray();

      multipleTargets = possibleTargetObjects.Count > 1;
    }
    
    protected override void BuildSelectionTree(OdinMenuTree tree) {
      tree.Config.DrawSearchToolbar = true;
      tree.Selection.SupportsMultiSelect = false;

      foreach (var res in source) {
        var path = multipleTargets
          ? $"{nicify(res.candidate.GetType().Name)}/{nicify(res.type.Name)}"
          : res.type.Name;
        tree.Add(path, res);
      }

      if (multipleTargets) {
        foreach (var component in source.Select(_ => _.candidate).Distinct()) {
          tree.Add(nicify(component.GetType().Name), null, componentIcon(component));
        }
      }
    }

    public static Texture componentIcon(Object component) => 
      EditorGUIUtility.ObjectContent(component, component.GetType()).image;

    static string nicify(string str) => ObjectNames.NicifyVariableName(str);
  }
}