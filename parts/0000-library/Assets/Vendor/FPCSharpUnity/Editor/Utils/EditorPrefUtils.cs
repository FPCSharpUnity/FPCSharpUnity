using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.Utils {
  public static class EditorPrefUtils {
    [MenuItem("Tools/FP C# Unity/Caching/Clear Cache")]
    public static void clearCache() {
      if (EditorUtility.DisplayDialog("Clear cache?", "Are you sure?", "Clear", "Cancel")) {
        Caching.ClearCache();
        Log.d.info("Cache cleared");
      }
    }
    
    [MenuItem("Tools/FP C# Unity/Prefs/Clear PlayerPrefs")]
    public static void clearPlayerPrefs() {
      if (EditorUtility.DisplayDialog("Clear player prefs?", "Are you sure?", "Clear", "Cancel")) {
        PlayerPrefs.DeleteAll();
        Log.d.info("Player prefs cleared");
      }
    }

    [MenuItem("Tools/FP C# Unity/Prefs/Clear EditorPrefs")]
    public static void clearEditorPrefs() {
      static bool check(string title) =>
        EditorUtility.DisplayDialog(
          "Clear editor prefs?",
          $"{title}\n\n" +
          $"This will delete all the settings you have set in Unity Editor " +
          $"(including SDK paths, color schemes and any data that custom plugins might have saved)\n\n" +
          $"!!! Use this as LAST RESORT only !!!",
          "Wipe them clean", "Cancel, I'm scared"
        );

      if (
        check("Are you sure?")
        && check("Are you really sure?")
        && check("You're sure you understand the consequences?")
      ) {
        EditorPrefs.DeleteAll();
        Log.d.info("Editor prefs cleared. Good luck with that.");
      }
    }
  }
}