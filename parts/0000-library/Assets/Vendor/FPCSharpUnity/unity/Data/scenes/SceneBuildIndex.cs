using FPCSharpUnity.core.macros;
using GenerationAttributes;
using UnityEngine.SceneManagement;

namespace FPCSharpUnity.unity.Data.scenes; 

/// <summary>Index of a scene in the build settings. Obtained via <see cref="Scene.buildIndex"/>.</summary>
[Record(ConstructorFlags.Constructor), NewTypeImplicitTo]
public readonly partial struct SceneBuildIndex { public readonly int index; }