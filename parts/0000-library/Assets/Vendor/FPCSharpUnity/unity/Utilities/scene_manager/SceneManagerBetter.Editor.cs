#if UNITY_EDITOR
using ExhaustiveMatching;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Data.scenes;
using GenerationAttributes;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities; 

public partial class SceneManagerBetter {
  /// <inheritdoc cref="Editor"/>
  [LazyProperty] public Editor __EDITOR => new(this);

  /// <summary>
  /// APIs for interacting with <see cref="EditorSceneManager"/>.
  /// </summary>
  public partial class Editor {
    readonly SceneManagerBetter backing;

    public Editor(SceneManagerBetter backing) => this.backing = backing;

    public AsyncSceneLoad loadSceneAsyncInPlayMode(
      ScenePath scenePath, LoadSceneParametersBetter loadSceneParams, bool automaticActivation
    ) =>
      automaticActivation
        ? loadSceneAsyncInPlayModeWithAutomaticActivation(scenePath, loadSceneParams)
        : loadSceneAsyncInPlayModeWithManualActivation(scenePath, loadSceneParams);
    
    /// <inheritdoc cref="SceneManagerBetter.loadSceneAsyncWithAutomaticActivation"/>
    /// <seealso cref="EditorSceneManager.LoadSceneAsyncInPlayMode"/>
    public AsyncSceneLoadWithAutomaticActivation loadSceneAsyncInPlayModeWithAutomaticActivation(
      ScenePath scenePath,
      LoadSceneParametersBetter parameters
    ) => backing.loadSceneAsyncWithAutomaticActivation(scenePath, parameters, startLoadingUsingEditorSceneManager);

    /// <inheritdoc cref="SceneManagerBetter.loadSceneAsyncWithManualActivation"/>
    /// <seealso cref="EditorSceneManager.LoadSceneAsyncInPlayMode"/>
    public AsyncSceneLoadWithManualActivation loadSceneAsyncInPlayModeWithManualActivation(
      ScenePath scenePath,
      LoadSceneParametersBetter parameters
    ) => backing.loadSceneAsyncWithManualActivation(scenePath, parameters, startLoadingUsingEditorSceneManager);

    public static readonly StartLoading startLoadingUsingEditorSceneManager =
      static (scenePath, loadSceneParams) => {
        var op = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, loadSceneParams);
        // When we do a `LoadSceneAsyncInPlayMode` that scene becomes the last scene in the `SceneManager` list.
        var scene = instance.getLastLoadedScene();
        return (op, scene);
      };

    /// <inheritdoc cref="EditorSceneManager.SaveOpenScenes"/>
    public bool saveOpenScenes() => EditorSceneManager.SaveOpenScenes();

    /// <inheritdoc cref="EditorSceneManager.OpenScene(string,OpenSceneMode)"/>
    public Scene openScene(ScenePath scenePath, OpenSceneMode openSceneMode=OpenSceneMode.Single) {
      switch (openSceneMode) {
        case OpenSceneMode.Single:
          backing.notifyAllScenesAboutUnloading(
            maybeExceptThisScene: None._,
            debugMethodName: nameof(Editor) + "." + nameof(openScene), debugData: scenePath
          );
          break;
        case OpenSceneMode.Additive:
        case OpenSceneMode.AdditiveWithoutLoading:
          break;
        default:
          throw ExhaustiveMatch.Failed(openSceneMode);
      }
      return EditorSceneManager.OpenScene(scenePath, openSceneMode);
    }

    /// <inheritdoc cref="EditorSceneManager.NewScene(NewSceneSetup,NewSceneMode)"/>
    public Scene newScene(NewSceneSetup setup, NewSceneMode mode) {
      switch (mode) {
        case NewSceneMode.Single:
          backing.notifyAllScenesAboutUnloading(
            maybeExceptThisScene: None._,
            debugMethodName: nameof(Editor) + "." + nameof(newScene), debugData: setup
          );
          break;
        case NewSceneMode.Additive:
          break;
        default:
          throw ExhaustiveMatch.Failed(mode);
      }
      return EditorSceneManager.NewScene(setup, mode);
    }

    /// <inheritdoc cref="EditorSceneManager.SaveScene(Scene,string,bool)"/>
    public bool saveScene(Scene scene, ScenePath path, bool saveAsCopy = false) => 
      EditorSceneManager.SaveScene(scene, path);
  }
}
#endif