using FPCSharpUnity.unity.Components.Interfaces;
using JetBrains.Annotations;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Logger;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Editor.Utils {
  /// <summary>
  /// Shows a modal editor window where a user can enter some text.
  /// </summary>
  public class EditorInputDialog : EditorWindow, IMB_OnGUI, IMB_OnDestroy {
    string dialogTitle, submitText, cancelText, currentValue = "";
    Option<string> helpText;
    Promise<Option<string>> promise;

    Vector2 helpTextScrollPos, inputTextScrollPos;

    /// <summary>
    /// Show the dialog and return the future with the value.
    /// </summary>
    /// <param name="title">Title text</param>
    /// <param name="helpText">Help text to show</param>
    /// <param name="submitText">Text for button that returns Some</param>
    /// <param name="cancelText">Text for button that returns None</param>
    /// <param name="windowCenterXPosition">Position in screen percentage (0-1) of the window center along X axis.</param>
    /// <param name="windowCenterYPosition">Position in screen percentage (0-1) of the window center along Y axis.</param>
    /// <param name="width">Width of the window in screen percentage (0-1).</param>
    /// <param name="height">Height of the window in screen percentage (0-1).</param>
    /// <returns>
    /// None if user pressed <see cref="cancelText"/> and Some if user pressed <see cref="submitText"/>.
    /// </returns>
    [PublicAPI] public static Future<Option<string>> show(
      string title, Option<string> helpText = default, string submitText = "Submit", string cancelText = "Cancel",
      float windowCenterXPosition = 0.5f, float windowCenterYPosition = 0.5f, float width = 0.8f, float height = 0.8f
    ) {
      var window = CreateInstance<EditorInputDialog>();
      var future = Future.async(out window.promise);
      window.dialogTitle = title;
      window.helpText = helpText;
      window.submitText = submitText;
      window.cancelText = cancelText;
      var resolution = Screen.currentResolution;

      // Window.position is broken when 'Preferences/Ui scaling' is not 100%.
      //
      // For example if we try to put window at x=100 and y=100 with 150% scaling, the window gets placed at x=150 and
      // y=150 instead.
      // 
      // We can fix this by dividing position by UI scale factor.
      var pixelsPerUnit = EditorGUIUtility.pixelsPerPoint;
      
      var displayWidth = resolution.width;
      var displayHeight = resolution.height;
      var windowWidth = displayWidth * width / pixelsPerUnit;
      var windowHeight = displayHeight * height / pixelsPerUnit;
      var windowX = displayWidth * windowCenterXPosition / pixelsPerUnit - windowWidth / 2;
      var windowY = displayHeight * windowCenterYPosition / pixelsPerUnit - windowHeight / 2;

      window.position = new Rect(windowX, windowY, windowWidth, windowHeight);
      window.ShowPopup();
      return future;
    }

    public void OnGUI() {
      EditorGUILayout.LabelField(dialogTitle, EditorStyles.boldLabel);
      GUILayout.Space(10);
      var helpStyle = new GUIStyle(EditorStyles.textArea) {wordWrap = true, fixedHeight = 250};
      var style = new GUIStyle(EditorStyles.textArea) {wordWrap = true};
      {if (helpText.valueOut(out var text)) {
        EditorGUILayout.LabelField("Help:");
        helpTextScrollPos = EditorGUILayout.BeginScrollView(helpTextScrollPos, GUILayout.MaxHeight(250));
        EditorGUILayout.TextArea(text, style, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Input:");
      }}
      inputTextScrollPos = EditorGUILayout.BeginScrollView(inputTextScrollPos, GUILayout.ExpandHeight(true));
      currentValue = EditorGUILayout.TextArea(currentValue, style);
      EditorGUILayout.EndScrollView();
      GUILayout.Space(10);
      GUILayout.BeginHorizontal();
      // Promise will become null after project recompilation and then clicking that button would be no good
      // so just close the window.
      if (promise == null) Close();
      
      if (GUILayout.Button(submitText)) {
        promise.tryComplete(Some.a(currentValue));
        Close();
      }
      else if (GUILayout.Button(cancelText)) {
        promise.tryComplete(None._);
        Close();
      }
      GUILayout.EndHorizontal();
    }

    public void OnDestroy() {
      promise.tryComplete(None._);
    }
  }
}