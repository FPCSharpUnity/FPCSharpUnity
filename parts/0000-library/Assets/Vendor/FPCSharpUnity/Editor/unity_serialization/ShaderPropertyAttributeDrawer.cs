using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.core.Utilities;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.unity_serialization {
  // This class was created by reference from 'ValueDropdown', by Odin inspector.
  // These 'DrawPriority' values are copied from 'ValueDropdown'.
  [DrawerPriority(0.0, 0.0, 2002.0)]
  public class ShaderPropertyAttributeDrawer : OdinAttributeDrawer<ShaderPropertyAttribute> {
    Func<IEnumerable<string>> getAllShaderPropertyValues;
    Action<string> mutatePropertyValue;
    Func<Option<Renderer>> getMaybeRenderer;
    Func<string> getSelectedValue;

    protected override void Initialize() {
      getMaybeRenderer = () => ValueResolver
        .Get<object>(Property, Attribute.rendererGetter)
        .GetValue()?.downcast(default(Renderer)) ?? None._;
      getAllShaderPropertyValues = () => getMaybeRenderer().valueOut(out var renderer)
        ? ShaderUtilsEditor.computeAllShaderPropertyNamesForType(
          new ShaderUtilsEditor.ComputeFromRenderer(renderer).up, Attribute.forType
        )
        : Array.Empty<string>();
      mutatePropertyValue = newValue => Property.ValueEntry.WeakValues[0] = newValue;
      getSelectedValue = () => (string) Property.ValueEntry.WeakValues[0];
    }

    // We only draw this attribute on string type.
    public override bool CanDrawTypeFilter(Type type) => type == typeof(string);
  
    protected override void DrawPropertyLayout(GUIContent label) {
      var selectedValue = getSelectedValue();
      var maybeValidationError = ShaderUtilsEditor.validateShaderProperty(
        getMaybeRenderer(), shaderPropertyName: selectedValue, Attribute.forType
      );
      
      // We draw this property in red color if we have validation errors.
      // To not draw sequent properties in editor in red color.
      // We cache this value and set it later on. 
      var prevColor = GUI.color;

      #region PropertyDrawing

      {if (maybeValidationError.valueOut(out var validationError)) {
        EditorGUILayout.HelpBox(validationError.message, MessageType.Error);
        GUI.color *= Color.red;
      }}
      drawLabelAndDropdown();

      #endregion
      
      // Resetting inspector color, after properties were drawn.
      GUI.color = prevColor;

      void drawLabelAndDropdown() {
        // This includes full area where label and dropdown should be drawn.
        var fullRect = EditorGUILayout.GetControlRect(hasLabel: false);
        // Drawing label.
        EditorGUI.LabelField(fullRect, label);
        // Calculating dropdown area.
        var dropdownArea = computeDropdownArea(fullRect);
        // Draws dropdown witch opens selector in corresponding position to dropdown.
        OdinSelector<string>.DrawSelectorDropdown(dropdownArea, selectedValue, openSelector);
      }
    }

    // This computation was taken from 'Odin Inspector's 'ValueDropdown' code implementation.
    static Rect computeDropdownArea(Rect rect) {
      var dropdownArea = rect;
      var currentIndentAmount = GUIHelper.CurrentIndentAmount;
      var newRect = new Rect(rect.x, rect.y, GUIHelper.BetterLabelWidth - currentIndentAmount, rect.height);
      dropdownArea.xMin = newRect.xMax;

      return dropdownArea;
    }

    /// <summary>Opens selector with search field.</summary>
    OdinSelector<string> openSelector(Rect rect) {
      var selector = new GenericSelector<string>(title: "", supportsMultiSelect: false, getAllShaderPropertyValues());

      selector.EnableSingleClickToSelect();
      selector.SelectionTree.SortMenuItemsByName();
      selector.SetSelection(getSelectedValue());
      selector.SelectionTree.Config.DrawSearchToolbar = true;
      selector.SelectionTree.DefaultMenuStyle.Height = 22;
      selector.SelectionConfirmed += selection => mutatePropertyValue(selection.FirstOrDefault());
      
      var window = selector.ShowInPopup(rect);
      window.OnClose += selector.SelectionTree.Selection.ConfirmSelection;

      return selector;
    }
  }
}