#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FPCSharpUnity.core.exts;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Vendor.FPCSharpUnity.unity.Utilities;

/// <summary>
/// Allows to quickly create a dropdown popup, where you can select an item from that list.
/// </summary>
public class GenericOdinSelector<A> : OdinSelector<A> {
  readonly IEnumerable<A> valuesToPickFrom;
  readonly Func<A, string> getName;
  readonly Func<A, Sprite> getIcon;

  /// <param name="valuesToPickFrom"></param>
  /// <param name="getName">Will use ToString() by default.</param>
  /// <param name="getIcon">Won't show an icon be default.</param>
  GenericOdinSelector(
    IEnumerable<A> valuesToPickFrom, Func<A, string> getName = null, Func<A, Sprite> getIcon = null
  ) {
    this.valuesToPickFrom = valuesToPickFrom;
    this.getName = getName ?? (a => a.ToString());
    this.getIcon = getIcon;
  }

  /// <param name="valuesToPickFrom"></param>
  /// <param name="onSelect">Callback when user selects an item and selection popup closes.</param>
  /// <param name="getName">Will use ToString() by default.</param>
  /// <param name="getIcon">Won't show an icon be default.</param>
  public static void showPopup(
    IEnumerable<A> valuesToPickFrom, Action<A> onSelect, Func<A, string> getName = null, 
    Func<A, Sprite> getIcon = null
  ) {
    var selector = new GenericOdinSelector<A>(valuesToPickFrom, getIcon: getIcon, getName: getName);
    selector.SelectionConfirmed += selection => {
      {if (selection != null && selection.headOption().valueOut(out var selectedValue)) {
        onSelect(selectedValue);
      }}
    };
    selector.ShowInPopup();
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
  }
}

public static class GenericOdinSelector {
  /// <inheritdoc cref="GenericOdinSelector{A}.showPopup"/>
  public static void showPopup<A>(
    IEnumerable<A> valuesToPickFrom, Action<A> onSelect, Func<A, string> getName = null,
    Func<A, Sprite> getIcon = null
  ) =>
    GenericOdinSelector<A>.showPopup(valuesToPickFrom: valuesToPickFrom, onSelect: onSelect, getName: getName,
      getIcon: getIcon
    );
}
#endif