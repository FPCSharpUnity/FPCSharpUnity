using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.core.exts;
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
    
    List<bool> enabledResolvers = new();

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
    static bool useDefaultResolver = true;
    static readonly List<AssetReferences.BytesParserAndGuidResolver> enabledExtraResolvers = new();

    public static void processFiles(AssetUpdate data) {
      if (!enabled.value) return;

      var extraResolvers = enabledExtraResolvers.toImmutableArrayC();
      
      foreach (var extraParser in extraResolvers._unsafeArray) {
        extraParser.initBeforeParsing();
      }
      
      worker.EnqueueItem(() => {
        try {
          process(data, log, extraResolvers);
        }
        catch (Exception e) {
          log.error(e);
        }
      });
    }

    static void process(
      AssetUpdate data, ILog log,
      ImmutableArrayC<AssetReferences.BytesParserAndGuidResolver> extraResolvers
    ) {
      try {
        processing = true;
        needsRepaint = true;

        refsOpt.voidFoldM(
          () => refsOpt = Some.a(AssetReferences.a(
            data, progress, log, useDefaultResolver: useDefaultResolver,
            extraResolvers: extraResolvers
          )),
          refs => refs.update(data, progress, log)
        );
      }
      finally {
        processing = false;
      }
    }

    bool foldoutSelected = true;
    bool foldout1 = true, foldout2 = true, foldout3 = true, foldout4 = true, foldout5 = true;
    Object hoverItem, previousHoverItem;

    bool locked;
    Object[] selectedObjects = {};
    bool showActions, showChains;
    
    string searchQuery = "";
    string searchFolder = "";
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
        if (!enabled.value) {
          GUILayout.Label("You can disable custom resolvers before enabling.");
          {
            useDefaultResolver = EditorGUILayout.Toggle("Default Resolver", useDefaultResolver);
          }
          
          var extraResolvers = AssetReferences.extraResolversForProject;
          if (extraResolvers.Count != enabledResolvers.Count) {
            enabledResolvers.Clear();
            foreach (var _ in extraResolvers) {
              enabledResolvers.Add(true);
            }
            refreshEnabledList();
          }
          EditorGUI.BeginChangeCheck();
          for (var i = 0; i < enabledResolvers.Count; i++) {
            enabledResolvers[i] = EditorGUILayout.Toggle(extraResolvers[i].GetType().Name, enabledResolvers[i]);
          }
          if (EditorGUI.EndChangeCheck()) refreshEnabledList();
          refreshEnabledList();

          void refreshEnabledList() {
            enabledExtraResolvers.Clear();
            for (var i = 0; i < enabledResolvers.Count; i++) {
              if (enabledResolvers[i]) {
                enabledExtraResolvers.Add(extraResolvers[i]);
              }
            }
          }
        }
        else {
          drawWhenEnabled();
        }
        
        void drawWhenEnabled() {
          foreach (var refs in refsOpt) {
            GUILayout.Label($"Scan duration: {refs.scanDuration}");
          }
          locked = EditorGUILayout.Toggle("Lock", locked);
          showActions = EditorGUILayout.Toggle("Show Actions", showActions);
          showChains = EditorGUILayout.Toggle("Show Dependency Chains", showChains);
          if (!locked) {
            selectedObjects = Selection.objects;
          }
          if (selectedObjects.Length == 0) return;
          var selectedPaths = selectedObjects.Select(AssetDatabase.GetAssetPath).Where(_ => _ != null).ToArray();
          var selectedGuids = selectedPaths.Select(_ => new AssetGuid(AssetDatabase.AssetPathToGUID(_))).ToArray();
          if (selectedGuids.Length == 0) return;
          var firstGuid = selectedGuids[0];
          foldoutSelected = EditorGUILayout.Foldout(foldoutSelected, "Selected");
          if (foldoutSelected) {
            foreach (var guid in selectedGuids) {
              objectDisplay(guid);
            }
          }
          refsOpt.ifSomeM(refs => {
            displayObjects(
              selectedGuids, "Used by objects (parents)", refs.parents, ref foldout1, 
              getTags: selectedGuids.Length == 1 
                ? parentGuid => getTags(parent: parentGuid, child: firstGuid)
                : _ => ""
            );
            displayObjects(
              selectedGuids, "Contains (children)", refs.children, ref foldout2,
              getTags: selectedGuids.Length == 1 
                ? childGuid => getTags(parent: firstGuid, child: childGuid)
                : _ => ""
            );
            displayObjects("Placed in scenes", refs.findParentScenes(selectedGuids), ref foldout3);
            displayObjects("Placed in resources", refs.findParentResources(selectedGuids), ref foldout4);

            string getTags(AssetGuid parent, AssetGuid child) {
              if (AssetDatabaseUtils.loadMainAssetByGuid(child) is Texture2D) {
                return refs.children.children.get(parent)
                  .flatMapM(_ => _.guidsInFile.get(child))
                  .mapM(set => {
                    var containsTexture = set.Contains(FileId.texture);
                    // Sprite references will have random file ids.
                    var containsSprite = set.Count > (containsTexture ? 1 : 0);
                    return (containsTexture, containsSprite) switch {
                      (true, true) => "T S",
                      (true, false) => "T",
                      (false, true) => "S",
                      _ => ""
                    };
                  })
                  .getOrElse("");
              }
              else return "";
            }

            {
              EditorGUI.BeginChangeCheck();
              searchQuery = EditorGUILayout.TextField("Custom search query", searchQuery);
              searchFolder = EditorGUILayout.TextField("Custom search folder", searchFolder);
              if (EditorGUI.EndChangeCheck()) {
                searchResults.Clear();
                if (searchQuery.Length > 0) {
                  var folders = searchFolder.Length > 0 ? new[] {searchFolder} : Array.Empty<string>();
                  var paths = AssetDatabase.FindAssets(searchQuery, folders)
                    .Select(_ => new AssetPath(AssetDatabase.GUIDToAssetPath(_)));
                  foreach (var path in paths) {
                    searchResults.Add(path);
                  }
                }
              }

              searchDirection = (AssetReferences.ChildOrParent) EditorGUILayout.EnumPopup(
                "Custom search direction", searchDirection
              );

              var chains = refs.findDependencyChains(selectedGuids, searchDirection, path => searchResults.Contains(path));
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
      ReadOnlySpan<AssetGuid> selectedGuids, string name, AssetReferences.Neighbors neighbors, ref bool foldout,
      Func<AssetGuid, string> getTags
    ) {
      if (selectedGuids.Length == 0) {
        var guid = selectedGuids[0];
        {if (neighbors.getNeighbors(guid).valueOut(out var assets)) {
          displayObjects(name, assets, _ => _, _ => ImmutableList<AssetGuid>.Empty, ref foldout, getTags);
        } else {
          GUILayout.Label(name + " 0");
        }}
      }
      else {
        var items = new HashSet<AssetGuid>();
        foreach (var guid in selectedGuids) {
          neighbors.getNeighbors(guid).ifSomeM(assets => {
            foreach (var asset in assets) {
              items.Add(asset);
            }
          });
        }
        if (items.Count > 0) {
          displayObjects(name, items, _ => _, _ => ImmutableList<AssetGuid>.Empty, ref foldout, getTags);
        }
        else {
          GUILayout.Label(name + " 0");
        }
      }
    }

    void displayObjects(
      string name, IReadOnlyCollection<AssetReferences.Chain> chains, ref bool foldout
    ) => displayObjects(name, chains, _ => _.mainGuid, _ => _.guids, ref foldout, getTags: _ => "");
    
    void displayObjects<A>(
      string name, IReadOnlyCollection<A> datas, Func<A, AssetGuid> getMainGuid, 
      Func<A, ImmutableList<AssetGuid>> getChain, ref bool foldout,
      Func<AssetGuid, string> getTags
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
            objectDisplay(guid, tags: getTags(guid));
            
            if (showChains) {
              var chain = getChain(a);
              var first = true;
              foreach (var chainGuid in chain) {
                // skip first element in chain because we've already rendered it.
                if (first) {
                  first = false;
                  continue;
                }
                objectDisplay(chainGuid, indent: 1);
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

    void objectDisplay(AssetGuid guid, string tags = "", uint indent = 0) {
      EditorGUILayout.BeginHorizontal();
      if (indent != 0) GUILayout.Space(indent * 20);
      if (tags.nonEmpty()) {
        GUILayout.Label(tags, GUILayout.ExpandWidth(false));
      }
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
    }

    public void OnDisable() => tracker.Dispose();
  }
}