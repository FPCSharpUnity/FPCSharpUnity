#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Utilities;
using Sirenix.OdinInspector;
using UnityEditor;

namespace FPCSharpUnity.unity.unity_serialization;

public abstract partial class SerializableDictionaryBase<A, B> {
  [
    ShowInInspector, PropertyOrder(-999), OnValueChanged(nameof(_editor_onSearchStringChange))
  ] string _editor_search = "";
  [
    ShowInInspector, PropertyOrder(-999), ShowIf(nameof(_editor_showSearchResult)), 
    ListDrawerSettings(ShowPaging = true, Expanded = true)
  ] Dictionary<A, B> _editor_searchResult = new();
  bool _editor_showSearchResult => _editor_search?.Length > 0;

  void _editor_onSearchStringChange() => _editor_searchResult = 
    _editor_search != null
    ? _keyValuePairs
        .Where(_ => 
          _.key.ToString().Contains(_editor_search, StringComparison.InvariantCultureIgnoreCase)
          || _.value.ToString().Contains(_editor_search, StringComparison.InvariantCultureIgnoreCase)
        )
        .ToDictionary(a => a.key, a => a.value)
    : new Dictionary<A, B>();
  
  public void _editor_addValue(UnityEngine.Object parentComponent, A key, B value) {
    parentComponent.recordEditorChanges("Add value");
    var list = _keyValuePairs.ToList();
    list.Add(new Pair(key, value));
    _keyValuePairs = list.ToArray();
    valueChanged();
  }
  
  public void _editor_clear(UnityEngine.Object parentComponent) {
    parentComponent.recordEditorChanges("Clear dictionary");
    _keyValuePairs = [];
    valueChanged();
  }
}
#endif