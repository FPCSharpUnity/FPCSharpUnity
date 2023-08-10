#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.core.exts;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Vendor.FPCSharpUnity.unity.Utilities;

/// <summary>
/// Allows to quickly create a dropdown popup, where you can select an item from that list.
/// </summary>
public class GenericOdinSelector<A> : OdinSelector<A> {
  readonly IEnumerable<A> valuesToPickFrom;
  readonly Func<A, string> getName;
  readonly Func<A, Sprite> getIcon;
  readonly string initialSearchTerm;

  /// <param name="valuesToPickFrom"></param>
  /// <param name="getName">Will use ToString() by default.</param>
  /// <param name="getIcon">Won't show an icon be default.</param>
  GenericOdinSelector(
    IEnumerable<A> valuesToPickFrom, Func<A, string> getName = null, Func<A, Sprite> getIcon = null,
    string initialSearchTerm = ""
  ) {
    this.valuesToPickFrom = valuesToPickFrom;
    this.getName = getName ?? (a => a.ToString());
    this.getIcon = getIcon;
    this.initialSearchTerm = initialSearchTerm;
  }

  /* doesn't work!
   protected override float DefaultWindowWidth() {
    var resolution = Screen.currentResolution;
    var hasLongNames = valuesToPickFrom.Max(v => getName(v).Split('/').Max(str => str.Length)) > 50;
    return hasLongNames ? resolution.width / 2f : 350f;
  }*/

  /// <param name="valuesToPickFrom"></param>
  /// <param name="onSelect">Callback when user selects an item and selection popup closes.</param>
  /// <param name="getName">Will use ToString() by default.</param>
  /// <param name="getIcon">Won't show an icon be default.</param>
  public static void showPopup(
    IEnumerable<A> valuesToPickFrom, Action<A> onSelect, Func<A, string> getName = null, 
    Func<A, Sprite> getIcon = null, string initialSearchTerm = ""
  ) {
    var selector = new GenericOdinSelector<A>(valuesToPickFrom, getIcon: getIcon, getName: getName);
    selector.SelectionConfirmed += selection => {
      {if (selection != null && selection.headOption().valueOut(out var selectedValue)) {
        onSelect(selectedValue);
      }}
    };
    
    var window = selector.ShowInPopup();

    // Window.position is broken when 'Preferences/Ui scaling' is not 100%.
    //
    // For example if we try to put window at x=100 and y=100 with 150% scaling, the window gets placed at x=150 and
    // y=150 instead.
    // 
    // We can fix this by dividing position by UI scale factor.
    var pixelsPerUnit = EditorGUIUtility.pixelsPerPoint;
    
    var resolution = Screen.currentResolution;
    var displayWidth = resolution.width;
    var displayHeight = resolution.height;

    selector.SelectionTree.Config.SearchTerm = initialSearchTerm;

    EditorApplication.update += adjustPosition;

    void adjustPosition() {
      if (window) {
        var pos = window.position;
        var windowX = Mathf.Clamp(pos.x * pixelsPerUnit, 0, displayWidth - pos.width * pixelsPerUnit) / pixelsPerUnit;
        var windowY = Mathf.Clamp(pos.y * pixelsPerUnit, 0, displayHeight - pos.height * pixelsPerUnit) / pixelsPerUnit;
        //Debug.LogError($"{resolution.echo()}, {pos}, {windowX.echo()}, {windowY.echo()}, {pixelsPerUnit.echo()}");
        window.position = new Rect(windowX, windowY, pos.width, pos.height);
      }
      else EditorApplication.update -= adjustPosition;
    }
  }

  protected override void BuildSelectionTree(OdinMenuTree tree) {
    tree.Config.DrawSearchToolbar = true;
    tree.Selection.SupportsMultiSelect = false;
    tree.DefaultMenuStyle.IconSize = 36;
    tree.DefaultMenuStyle.Height = 40;
    foreach (var a in valuesToPickFrom) {
      if (getIcon != null) tree.Add(getName(a), a, getIcon(a));
      else tree.Add(getName(a), a);
    }
    SelectionTree.Config.SearchTerm = initialSearchTerm;
  }
}

public static class GenericOdinSelector {
  /// <inheritdoc cref="GenericOdinSelector{A}.showPopup"/>
  public static void showPopup<A>(
    IEnumerable<A> valuesToPickFrom, Action<A> onSelect, Func<A, string> getName = null,
    Func<A, Sprite> getIcon = null, string initialSearchTerm = ""
  ) =>
    GenericOdinSelector<A>.showPopup(valuesToPickFrom: valuesToPickFrom, onSelect: onSelect, getName: getName,
      getIcon: getIcon, initialSearchTerm: initialSearchTerm
    );
}
#endif