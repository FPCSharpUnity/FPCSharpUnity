using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.editor;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Logger {
  /// <summary>
  /// Allows you to control log levels 
  /// </summary>
  [HasLogger]
  public partial class LogLevelControlWindow : EditorWindow, IMB_OnGUI {
    [MenuItem("Tools/FP C# Unity/Log Level Control")]
    public static void OpenWindow() => GetWindow<LogLevelControlWindow>("Log Level Control").Show();
    
    Vector2 scrollPosition;
    readonly EditorGuiTableDrawer tableDrawer = new EditorGuiTableDrawer(new [] { 300, 100, 120, 100, 10000 });
    
    public void OnGUI() {
      var registry = Log.registry;
      var registered = registry.registered;
      var levels = EnumUtils.GetValuesArray<LogLevel>();
        
      using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition)) {
        tableDrawer.beginRow();
        tableDrawer.beginCell(); EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
        tableDrawer.beginCell(); EditorGUILayout.LabelField("Current Level", EditorStyles.boldLabel);

        tableDrawer.beginCell(); EditorGUILayout.LabelField("Current Override", EditorStyles.boldLabel);

        tableDrawer.beginCells(2); EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        if (registered.Count == 0) {
          tableDrawer.beginRow();
          tableDrawer.beginCells(5); 
          EditorGUILayout.LabelField("No loggers found in the default registry.");
        }
        else {
          foreach (var (name, log) in registered.OrderBySafe(_ => _.Key.name)) {
            tableDrawer.beginRow();
            
            var prefVal = LogLevelControl.prefValDict[name];
            using (new EditorGUILayout.HorizontalScope()) {
              tableDrawer.beginCell(); EditorGUILayout.LabelField(
                name.name, name == Log.DEFAULT_LOGGER_NAME ? EditorStyles.boldLabel : EditorStyles.label
              );
              tableDrawer.beginCell(); EditorGUILayout.LabelField(log.level.ToString());
              tableDrawer.beginCell(); EditorGUILayout.LabelField(prefVal.value.ToString());

              tableDrawer.beginCell();
              EditorGUILayout.Space(10);
              if (GUILayout.Button("Clear")) {
                LogLevelControl.prefValDict[name].value = None._;
              }
              EditorGUILayout.Space(10);

              tableDrawer.beginCell();
              EditorGUILayout.Space(10);
              foreach (var level in levels) {
                if (GUILayout.Button(level.ToString())) {
                  LogLevelControl.prefValDict[name].value = Some.a(level);
                  log.level = level;
                }
              }
              EditorGUILayout.Space(10);
            }
          }
        }

        scrollPosition = scrollScope.scrollPosition;
      }
      
      tableDrawer.endTable();
    }
  }
}