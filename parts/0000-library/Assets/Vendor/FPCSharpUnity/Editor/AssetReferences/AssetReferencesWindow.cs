using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;

using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Data;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Editor.AssetReferences {
  class AssetReferencesAssetProcessor : AssetPostprocessor {
    [UsedImplicitly]
    static void OnPostprocessAllAssets(
      string[] importedAssets, string[] deletedAssets, string[] movedAssets,
      string[] movedFromAssetPaths
    ) {
      if (!AssetReferencesWindow.enabled.value) return;

      var data = new AssetUpdate(
        importedAssets.Select(p => new AssetPath(p)).ToImmutableArray(),
        deletedAssets.Select(p => new AssetPath(p)).ToImmutableArray(),
        movedFromAssetPaths
          .zip(movedAssets, (i1, i2) => new AssetUpdate.Move(new AssetPath(i1), new AssetPath(i2))).ToImmutableArray()
      );

      AssetReferencesWindow.processFiles(data);
    }
  }

  // Ugly code ahead 🚧
  public class AssetReferencesWindow : EditorWindow, IMB_OnGUI, IMB_Update, IMB_OnEnable, IMB_OnDisable {
    [LazyProperty, Implicit] static ILog log => Log.d.withScope(nameof(AssetReferencesWindow));

    Vector2 scrollPos;
    // disables automatically on code refresh
    // code refresh happens on code change and when entering play mode
    public static readonly IRxRef<bool> enabled = RxRef.a(false);

    [MenuItem("Tools/Window/Asset References")]
    public static void init() {
      // Get existing open window or if none, make a new one:
      var window = GetWindow<AssetReferencesWindow>("Asset References");
      window.Show();
    }

    // ReSharper disable once NotAccessedField.Local
    static ISubscription enabledSubscription;

    [InitializeOnLoadMethod, UsedImplicitly]
    static void initTasks() {
      enabledSubscription = enabled.subscribe(NoOpDisposableTracker.instance, b => {
        if (b) {
          refsOpt = None._;
          processFiles(AssetUpdate.fromAllAssets(
            AssetDatabase.GetAllAssetPaths().Select(p => new AssetPath(p)).ToImmutableArray()
          ));
        }
      });
    }

    static readonly Ref<float> progress = Ref.a(0f);
    static volatile bool processing, needsRepaint;
    static Option<AssetReferences> refsOpt;
    static readonly PCQueue worker = new PCQueue(1);

    public static void processFiles(AssetUpdate data) {
      if (!enabled.value) return;
      
      foreach (var extraParser in AssetReferences.extraResolvers) {
        extraParser.initBeforeParsing();
      }
      
      worker.EnqueueItem(() => {
        try {
          process(data, log);
        }
        catch (Exception e) {
          log.error(e);
        }
      });
    }

    static void process(AssetUpdate data, ILog log) {
      try {
        processing = true;
        needsRepaint = true;

        refsOpt.voidFoldM(
          () => refsOpt = Some.a(AssetReferences.a(data, progress, log, useExtraResolvers: true)),
          refs => refs.update(data, progress, log)
        );
      }
      finally {
        processing = false;
      }
    }

    bool foldout1 = true, foldout2 = true, foldout3 = true, foldout4 = true, foldout5 = true;
    Object hoverItem, previousHoverItem, lockedObj;
    readonly IRxRef<bool> locked = RxRef.a(false);
    bool showActions, showChains;
    
    string searchQuery = "";
    AssetReferences.ChildOrParent searchDirection = AssetReferences.ChildOrParent.Child;
    HashSet<string> searchResults = new();

    public void OnGUI() {
      var isMouseMoveEvent = Event.current.type == EventType.MouseMove;
      if (isMouseMoveEvent) hoverItem = null;
      if (processing) {
        EditorGUI.ProgressBar(new Rect(10, 10, position.width - 20, 20), progress.value, "Processing");
      }
      else {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        enabled.value = EditorGUILayout.Toggle("Enabled", enabled.value);
        foreach (var _ in enabled.value.opt(F.unit)) {
          foreach (var refs in refsOpt) {
            GUILayout.Label($"Scan duration: {refs.scanDuration}");
          }
          locked.value = EditorGUILayout.Toggle("Lock", locked.value);
          showActions = EditorGUILayout.Toggle("Show Actions", showActions);
          showChains = EditorGUILayout.Toggle("Show Dependency Chains", showChains);
          var cur = locked.value ? lockedObj : Selection.activeObject;
          if (cur == null) break;
          var curPath = AssetDatabase.GetAssetPath(cur);
          if (curPath == null) break;
          var currentGUID = new AssetGuid(AssetDatabase.AssetPathToGUID(curPath));
          if (currentGUID.guid == null) break;
          GUILayout.Label("Selected");
          objectDisplay(currentGUID);
          refsOpt.ifSomeM(refs => {
            displayObjects(currentGUID, "Used by objects (parents)", refs.parents, ref foldout1);
            displayObjects(currentGUID, "Contains (children)", refs.children, ref foldout2);
            displayObjects("Placed in scenes", refs.findParentScenes(currentGUID), ref foldout3);
            displayObjects("Placed in resources", refs.findParentResources(currentGUID), ref foldout4);

            {
              EditorGUI.BeginChangeCheck();
              searchQuery = EditorGUILayout.TextField("Custom search query", searchQuery);
              if (EditorGUI.EndChangeCheck()) {
                searchResults.Clear();
                if (searchQuery.Length > 0) {
                  var paths = AssetDatabase.FindAssets(searchQuery)
                    .Select(_ => new AssetPath(AssetDatabase.GUIDToAssetPath(_)));
                  foreach (var path in paths) {
                    searchResults.Add(path);
                  }
                }
              }

              searchDirection = (AssetReferences.ChildOrParent) EditorGUILayout.EnumPopup(
                "Custom search direction", searchDirection
              );

              var chains = refs.findDependencyChains(currentGUID, searchDirection, path => searchResults.Contains(path));
              displayObjects("Custom search results:", chains, ref foldout5);
            }
          });
          if (!isMouseMoveEvent && hoverItem) {
            GUI.Label(new Rect(Event.current.mousePosition, new Vector2(128, 128)), AssetPreview.GetAssetPreview(previousHoverItem));
          }
        }
        EditorGUILayout.EndScrollView();
      }
      if (isMouseMoveEvent) {
        if (previousHoverItem != hoverItem || hoverItem) Repaint();
        previousHoverItem = hoverItem;
      }
    }

    void displayObjects(
      AssetGuid curGuid, string name, Dictionary<AssetGuid, HashSet<AssetGuid>> dict, ref bool foldout
    ) {
      if (dict.ContainsKey(curGuid)) {
        displayObjects(name, dict[curGuid], _ => _, _ => ImmutableList<AssetGuid>.Empty, ref foldout);
      }
      else {
        GUILayout.Label(name + " 0");
      }
    }

    void displayObjects(
      string name, IReadOnlyCollection<AssetReferences.Chain> chains, ref bool foldout
    ) => displayObjects(name, chains, _ => _.mainGuid, _ => _.guids, ref foldout);
    
    void displayObjects<A>(
      string name, IReadOnlyCollection<A> datas, Func<A, AssetGuid> getMainGuid, 
      Func<A, ImmutableList<AssetGuid>> getChain, ref bool foldout
    ) {
      foldout = EditorGUILayout.Foldout(foldout, name + " " + datas.Count);
      if (foldout) {
        if (showActions) {
          IEnumerable<AssetGuid> guids() => datas.Select(getMainGuid);
          
          if (GUILayout.Button("select"))
            Selection.objects = loadGuids(guids()).ToArray();

          if (GUILayout.Button("set dirty")) {
            var objects = loadGuids(guids()).ToArray();
            objects.recordEditorChanges("Set objects dirty");
            foreach (var o in objects) EditorUtility.SetDirty(o);
          }
        }

        foreach (var a in datas.OrderBySafe(d => AssetDatabase.GUIDToAssetPath(getMainGuid(d)))) {
          var guid = getMainGuid(a);
          var asset = AssetDatabaseUtils.loadMainAssetByGuid(guid);
          if (asset != null) {
            objectDisplay(guid);
            
            if (showChains) {
              var chain = getChain(a);
              var first = true;
              foreach (var chainGuid in chain) {
                // skip first element in chain because we've already rendered it.
                if (first) {
                  first = false;
                  continue;
                }
                objectDisplay(chainGuid, 1);
              }
            }
          }
          else {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.isNullOrEmpty()) {
              GUILayout.Label($"Unknown guid: {guid}");
            }
            else {
              GUILayout.Label(AssetDatabase.GUIDToAssetPath(guid));
            }
          }
        }
      }
    }

    // ToArray in case someone modifies guids collection
    static IEnumerable<Object> loadGuids(IEnumerable<AssetGuid> guids) =>
      guids.ToArray().Select(guid => AssetDatabaseUtils.loadMainAssetByGuid(guid.guid));

    void objectDisplay(string guid, uint indent = 0) {
      EditorGUILayout.BeginHorizontal();
      if (indent != 0) GUILayout.Space(indent * 20);
      var obj = AssetDatabaseUtils.loadMainAssetByGuid(guid);
      EditorGUILayout.ObjectField(obj, typeof(Object), false);
      var etype = Event.current.type;
      if (etype == EventType.MouseMove) {
        var info = typeof(EditorGUILayout).GetField("s_LastRect", BindingFlags.NonPublic | BindingFlags.Static);
        // ReSharper disable once PossibleNullReferenceException
        var rect = (Rect)info.GetValue(null);
        var mousePos = Event.current.mousePosition;
        if (rect.Contains(mousePos)) {
          hoverItem = obj;
        }
      }

      if (GUILayout.Button("", GUILayout.MaxWidth(30))) {
        Selection.activeObject = obj;
      }
      EditorGUILayout.EndHorizontal();
    }

    public void Update() {
      if (processing) Repaint();
      else if (needsRepaint) {
        Repaint();
        needsRepaint = false;
      }
    }

    [UsedImplicitly]
    void OnSelectionChange() => Repaint();

    [LazyProperty] DisposableTracker tracker => new DisposableTracker();

    public void OnEnable() {
      wantsMouseMove = true;
      locked.subscribe(tracker, v => {
        if (v) lockedObj = Selection.activeObject;
      });
    }

    public void OnDisable() => tracker.Dispose();
  }
}