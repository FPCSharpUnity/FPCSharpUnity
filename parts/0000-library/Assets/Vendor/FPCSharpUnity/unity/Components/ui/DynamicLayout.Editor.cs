#if UNITY_EDITOR
using System;
using System.Linq;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.unity_serialization;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.ui;

public partial class DynamicLayout : IMB_OnDrawGizmosSelected {
  const string EDITOR_TEST = "Editor Test";
  
  static readonly Vector3[] editorCacheForArrayOfFour = new Vector3[4];
  public void OnDrawGizmosSelected() {
    if (!_maskRect || !_container | !_scrollRect) return;
    Gizmos.color = Color.red;
    _maskRect.GetWorldCorners(editorCacheForArrayOfFour);
    drawEdges();

    _container.GetWorldCorners(editorCacheForArrayOfFour);
    Gizmos.color = Color.yellow;
    drawEdges();
    var scale = 
      Application.isPlaying
      ? 1f
      : transform.lossyScale.x;
    // bottom left
    editorCacheForArrayOfFour[0] = new Vector3(
      editorCacheForArrayOfFour[0].x + _padding.left * scale, editorCacheForArrayOfFour[0].y + _padding.bottom * scale
    );
    // top left
    editorCacheForArrayOfFour[1] = new Vector3(
      editorCacheForArrayOfFour[1].x + _padding.left * scale, editorCacheForArrayOfFour[1].y - _padding.top * scale
    );
    // top right
    editorCacheForArrayOfFour[2] = new Vector3(
      editorCacheForArrayOfFour[2].x - _padding.right * scale, editorCacheForArrayOfFour[2].y - _padding.top * scale
    );
    // bottom right
    editorCacheForArrayOfFour[3] = new Vector3(
      editorCacheForArrayOfFour[3].x - _padding.right * scale, editorCacheForArrayOfFour[3].y + _padding.bottom * scale
    );
    Gizmos.color = Color.green;
    drawEdges();

    void drawEdges() {
      Gizmos.DrawLine(editorCacheForArrayOfFour[0], editorCacheForArrayOfFour[1]);
      Gizmos.DrawLine(editorCacheForArrayOfFour[2], editorCacheForArrayOfFour[1]);
      Gizmos.DrawLine(editorCacheForArrayOfFour[2], editorCacheForArrayOfFour[3]);
      Gizmos.DrawLine(editorCacheForArrayOfFour[0], editorCacheForArrayOfFour[3]);        
    }
  }
  
  [Button, HideInPlayMode, FoldoutGroup(EDITOR_TEST)] void editorQuickTest1() {
    editorTestPopulateListWithFullWidthEntries(1);
    _editorTestLayout = true;
    foreach (var entry in _editorTestEntries) {
      SceneVisibilityManager.instance.Hide(entry.item.gameObject, includeDescendants: true);
    }
  }
  
  [Button, HideInPlayMode, FoldoutGroup(EDITOR_TEST)] void editorQuickTestAll() {
    for (var i = 0; i < 20; i++) {
      editorTestPopulateListWithFullWidthEntries();
    }
    _editorTestLayout = true;
    foreach (var entry in _editorTestEntries) {
      SceneVisibilityManager.instance.Hide(entry.item.gameObject, includeDescendants: true);
    }
  }
  
  [Button, HideInPlayMode, FoldoutGroup(EDITOR_TEST)] void editorTestPopulateListWithFullWidthEntries(int number = 999) {
    _editorTestEntries = _editorTestEntries.Concat(
      _container.children()
        .Where(tr => (tr.gameObject.hideFlags & HideFlags.DontSave) != HideFlags.DontSave)
        .Take(number)
        .Select(tr => new EditorTestEntry(_item: (RectTransform)tr, _sizeInSecondaryAxis: Percentage.oneHundred,
          _customSizeInScrollableAxis: new UnityOption<float>(None._)
        ))  
    ).ToArray();
  }
  
  [Button, HideInPlayMode, FoldoutGroup(EDITOR_TEST)] void editorClearPreviews() {
    foreach (var entry in _editorTestEntries) {
      entry.clearPreview();
      SceneVisibilityManager.instance.Show(entry.item.gameObject, includeDescendants: true);
    }

    foreach (Transform tr in _container) {
      if ((tr.gameObject.hideFlags & HideFlags.DontSave) == HideFlags.DontSave) {
        DestroyImmediate(tr.gameObject);
      }
    }
  }

  [ShowInInspector, HideInPlayMode, FoldoutGroup(EDITOR_TEST)] EditorTestEntry[] _editorTestEntries = 
    new EditorTestEntry[0];
  [ShowInInspector, HideInPlayMode, FoldoutGroup(EDITOR_TEST)] bool _editorTestLayout;

  [ShowInInspector, HideInPlayMode, FoldoutGroup(EDITOR_TEST), PropertyRange(0, 1)] float editorTestScroll {
    get {
      if (!_scrollRect) return -1;
      return (_scrollRect.horizontal
        ? _scrollRect.horizontalNormalizedPosition
        : 1f - _scrollRect.verticalNormalizedPosition);
    }
    set {
      if (_scrollRect.horizontal) _scrollRect.horizontalNormalizedPosition = value;
      else _scrollRect.verticalNormalizedPosition = 1f - value;
    }
  }

  [OnInspectorGUI] void editorTestLayout() {
    try {
      if (!_editorTestLayout || Application.isPlaying) {
        editorClearPreviews();
        return;
      }
      var maskSize = _maskRect.rect;
      var containerSize = _container.rect;
      var isHorizontal = _scrollRect.horizontal;
      var entries = _editorTestEntries.Select(entry =>
        entry.maybeVisibleEntry.valueOut(out var e) ? e : new EditorTestEntry.DynamicLayoutElement(entry)
      ).toImmutableArrayC();

      var result = Init.forEachElement(
        spacing: _spacingInScrollableAxis,
        iElementDatas: entries,
        renderLatestItemsFirst: false, padding: _padding, isHorizontal: isHorizontal,
        containersRectTransform: _container,
        visibleRect: Init.calculateVisibleRectStatic(container: _container, maskRect: _maskRect), dataA: Unit._, 
        forEachElementAction: (layoutElement, placementVisible, cellRect, _) => {
          switch (placementVisible) {
            case true: {
              if (!layoutElement.visibleRt.valueOut(out var rt)) {
                var maybeRt = layoutElement.showOrUpdate(parent: (RectTransform)layoutElement.data.item.transform.parent, forceUpdate: true);
                // Item is setup as empty.
                if (!maybeRt.valueOut(out rt)) return;
              }
              Init.updateVisibleElement(
                layoutElement, rt, cellRect: cellRect, padding: padding, expandElements: _expandElements, 
                isHorizontal: isHorizontal, containerSize: containerSize
              );   
              break;
            }
            case false: {
              layoutElement.hide();
              break;
            }
          }
        }
      );

      Init.onRectSizeChange(
        container: _container, expandElements: _expandElements, isHorizontal: isHorizontal,
        containerSizeInScrollableAxis: result.containerSizeInScrollableAxis, rectSize: maskSize
      );
      
      // At runtime these are set by Unity automatically.
      if (isHorizontal) _scrollRect.verticalNormalizedPosition = 0f;
      else _scrollRect.horizontalNormalizedPosition = 0f;
    }
    catch (Exception e) {
      Log.d.error($"Error in {Macros.classAndMethodNameShort}:\n{e}");
      _editorTestLayout = false;
    }
  }

  /// <summary> Is used to draw debug test item previews in editor. </summary>
  [Serializable, Record] public partial class EditorTestEntry
  {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized
    [SerializeField, NotNull, PublicAccessor] RectTransform _item;
    [SerializeField, NotNull, PublicAccessor] Percentage _sizeInSecondaryAxis;
    [SerializeField, NotNull, PublicAccessor, InfoBox(
      $"If not provided, the {nameof(_item)} size will be used."
    )] UnityOption<float> _customSizeInScrollableAxis;
    // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

    /// <summary> Optional. Will be set to not null if this item is visible is layout. </summary>
    [ShowInInspector] public RectTransform visiblePreview => maybeVisibleEntry.flatMap(_ => _.visibleRt).getOrNull();
    [RecordExclude] public Option<DynamicLayoutElement> maybeVisibleEntry { get; private set; }
    
    [LazyProperty, Implicit] static ILog log => Log.d.withScope(nameof(EditorTestEntry));
    
    public void clearPreview() {
      if (visiblePreview) DestroyImmediate(visiblePreview.gameObject);
    }
    
    public class DynamicLayoutElement : ElementBase<EditorTestEntry, RectTransform> {
      public DynamicLayoutElement(
        EditorTestEntry data) : base(
        data, sizeProvider: data._customSizeInScrollableAxis.valueOut(out var size) 
          ? SizeProvider.Static.a(sizeInScrollableAxis: size, sizeInSecondaryAxis: data.sizeInSecondaryAxis)
          : SizeProvider.FromTemplateStatic.a(data._item, sizeInSecondaryAxis: data.sizeInSecondaryAxis), 
        maybeViewProvider: new ViewProvider.InstantiateAndDestroyEditor<RectTransform>(data._item), log
      ) {}

      protected override void becameVisible(RectTransform view, RectTransform rt, RectTransform parent) {
        base.becameVisible(view, rt, parent);
        view.name += " [Will not save]";
        view.gameObject.hideFlags = HideFlags.DontSave;
        data.maybeVisibleEntry = Some.a(this);
      }

      protected override void updateState(RectTransform view, ITracker tracker) {}

      protected override void becameInvisible() {
        base.becameInvisible();
        data.maybeVisibleEntry = None._;
      }
    }
  }
}
#endif