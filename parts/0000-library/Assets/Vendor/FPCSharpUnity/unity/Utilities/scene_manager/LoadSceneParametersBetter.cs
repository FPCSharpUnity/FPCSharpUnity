using GenerationAttributes;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Utilities;

/// <summary>
/// As <see cref="LoadSceneParameters"/> but has implicit conversion from <see cref="LoadSceneMode"/>.
/// </summary>
[Record(ConstructorFlags.Constructor)]
public readonly partial struct LoadSceneParametersBetter {
  public readonly LoadSceneMode loadSceneMode;
  public readonly LocalPhysicsMode localPhysicsMode;

  public LoadSceneParametersBetter(LoadSceneMode loadSceneMode) : this(loadSceneMode, LocalPhysicsMode.None) {}
  public static implicit operator LoadSceneParametersBetter(LoadSceneMode loadSceneMode) => new(loadSceneMode);
  public static implicit operator LoadSceneParameters(LoadSceneParametersBetter v) => new(v.loadSceneMode, v.localPhysicsMode);
}