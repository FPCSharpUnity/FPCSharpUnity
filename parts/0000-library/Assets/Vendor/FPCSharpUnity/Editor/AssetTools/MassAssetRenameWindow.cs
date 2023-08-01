using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.unity.Utilities;
using GenerationAttributes;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using UnityEditor;
using UnityEngine;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.Editor.AssetTools {
  public partial class MassAssetRenameWindow : EditorWindow, IMB_OnGUI {
    const string HUMAN_NAME = "Mass Asset Rename";
    [LazyProperty] static ILog log => Log.d.withScope(HUMAN_NAME);
    
    [MenuItem("Tools/Window/Mass Asset Rename")]
    public static void init() => GetWindow<MassAssetRenameWindow>(HUMAN_NAME).Show();

    /// <summary>String to search for in the asset name.</summary>
    [SerializeField] string _searchNeedle;
    [SerializeField] bool _needleIsRegex, _includeDirectories, _caseSensitive = true;

    /// <summary>String to replace the matched needle with.</summary>
    [SerializeField] string _replaceNeedleWith;

    /// <summary>All of the paths in the <see cref="AssetDatabase"/>.</summary>
    [SerializeField] PathStr[] _assetPaths = Array.Empty<PathStr>();
    
    /// <summary>
    /// Paths that we calculated that need replacement.
    /// <para/>
    /// The dictionary keys are directory paths where the `from` assets are located. That is all of the assets at one
    /// dictionary key will be located in the same directory.
    /// </summary>
    ImmutableDictionary<PathStr, ImmutableArrayC<(PathStr from, PathStr to)>> _replacementPaths = 
      ImmutableDictionary<PathStr, ImmutableArrayC<(PathStr from, PathStr to)>>.Empty;

    Vector2 _replacementPathsScrollViewPosition;

    public void OnGUI() {
      _includeDirectories = EditorGUILayout.Toggle("Include directories", _includeDirectories);
      _needleIsRegex = EditorGUILayout.Toggle("Search needle is regex", _needleIsRegex);
      if (_needleIsRegex) _caseSensitive = EditorGUILayout.Toggle("Case sensitive", _caseSensitive);
      _searchNeedle = EditorGUILayout.TextField(
        _needleIsRegex ? "Search regex" : "Search string",
        _searchNeedle
      );
      _replaceNeedleWith = EditorGUILayout.TextField("Replace with", _replaceNeedleWith);
      using (new EditorGUILayout.HorizontalScope()) {
        if (_assetPaths.nonEmpty()) {
          if (GUILayout.Button($"Search in {_assetPaths.Length} assets")) doSearch();
        }
        
        if (GUILayout.Button(_assetPaths.isEmpty() ? "Load asset paths" : "Reload asset paths")) loadAssetPaths();
      }

      if (_replacementPaths.nonEmpty()) {
        EditorGUILayout.Space(20);

        var asStr = _replacementPaths
          .OrderBySafe(_ => _.Key)
          .Select(kv => {
            var paths = kv.Value.Select(tpl => $"{s(tpl.from.basename)} -> {s(tpl.to.basename)}");
            return $"{s(kv.Key)}: {paths.mkStringEnumNewLines()}";
          })
          .mkString("\n");
        using (
          var scope = new EditorGUILayout.ScrollViewScope(
            _replacementPathsScrollViewPosition, GUILayout.ExpandHeight(true)
          )
        ) {
          EditorGUILayout.TextArea(asStr);
          _replacementPathsScrollViewPosition = scope.scrollPosition;
        }
        
        if (GUILayout.Button($"Rename ({_replacementPaths.Sum(_ => _.Value.Count)})")) doRename();
      }
    }

    void loadAssetPaths() {
      _assetPaths = AssetDatabase.GetAllAssetPaths().map(PathStr.a);
      if (_includeDirectories)
        _assetPaths = _assetPaths.SelectMany(p => new [] { p, p.dirname }).Distinct().ToArrayFast();
    }

    void doSearch() {
      var matcher = createMatcher();

      _replacementPaths = _assetPaths
        .collect(assetPath => matcher(assetPath).mapM(replaced => (assetPath, replaced)))
        .GroupBy(tpl => tpl.assetPath.dirname)
        .ToImmutableDictionary(
          kv => kv.Key,
          kv => kv.toImmutableArrayC()
        );
    }

    Matcher createMatcher() {
      if (_needleIsRegex) {
        var needleRe = new Regex(_searchNeedle, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        return assetPath => {
          var basename = assetPath.basename.asString();
          var match = needleRe.Match(basename);
          return match.Success 
            ? Some.a(newPath(assetPath, needleRe.Replace(basename, _replaceNeedleWith))) 
            : Option<PathStr>.None;
        };
      }
      else {
        return assetPath => {
          var basename = assetPath.basename.asString();
          return
            basename.Contains(_searchNeedle)
            ? Some.a(newPath(assetPath, basename.Replace(_searchNeedle, _replaceNeedleWith)))
            : Option<PathStr>.None;
        };
      }

      PathStr newPath(PathStr assetPath, string newFileName) => assetPath.dirname / newFileName;
    }

    void doRename() {
      using var _ = AssetDatabaseUtils.doAssetEditing();

      // Start from the longest paths to the shortest paths. The idea is that a shorter path will be a directory path
      // and a longer path will be files in that directory and we have to move files first and the directory later.
      //
      // If we move directory first, the file paths become invalid, thus we can't do that.
      var orderedReplacementPaths = 
        _replacementPaths.SelectMany(_ => _.Value).OrderByDescendingSafe(_ => _.from).ToArrayFast();
      foreach (var path in orderedReplacementPaths) {
        AssetDatabase.MoveAsset(path.from.unityPath, path.to.unityPath);
      }

      log.mInfo(
        $"Renamed: {orderedReplacementPaths.Select(tpl => $"{s(tpl.from)} -> {s(tpl.to)}").mkStringEnumNewLines()}"
      );
      
      _assetPaths = Array.Empty<PathStr>();
      _replacementPaths = ImmutableDictionary<PathStr, ImmutableArrayC<(PathStr from, PathStr to)>>.Empty;
    }

    /// <summary>Returns Some(replaced path) if the path matches.</summary>
    delegate Option<PathStr> Matcher(PathStr assetPath);
  }
}