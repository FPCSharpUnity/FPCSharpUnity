using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Dispose;
using FPCSharpUnity.unity.Logger;
using GenerationAttributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace FPCSharpUnity.unity.Utilities; 

/// <summary>
/// Safe version of APIs in <see cref="RenderSettings"/>.
/// </summary>
[HasLogger]
public static partial class RenderSettingsSafe {
  /// <summary>
  /// Allows you to change the global reflection mode with a bit more safety.
  /// </summary>
  public static readonly ActivationTracker<ReflectionMode> reflectionMode = 
    // Because the reflection mode is global, there never should be more than one activation, as otherwise they will
    // clash with each other.
    ActivationTracker.singleActivation<ReflectionMode>(log);

  static RenderSettingsSafe() {
    setupReflectionMode();
    
    void setupReflectionMode() {
      Option<(DefaultReflectionMode defaultReflectionMode, Cubemap customReflection)> original = None._;
      
      reflectionMode.activated.subscribe(
        DisposableTrackerU.disposeOnExitPlayMode,
        maybeCustomReflection => {
          if (maybeCustomReflection.valueOut(out var customReflection)) {
            // Store the last value.
            original = Some.a((RenderSettings.defaultReflectionMode, RenderSettings.customReflection));
            
            log.mDebug($"Changing reflection mode to {customReflection} ({original.echo()})");
            RenderSettings.defaultReflectionMode = customReflection.mode;
            RenderSettings.customReflection = customReflection.maybeCustomReflection.getOrNull();
          }
          else {
            if (original.valueOut(out var tpl)) {
              log.mDebug($"Restoring the reflection mode to {tpl}");
              RenderSettings.defaultReflectionMode = tpl.defaultReflectionMode;
              RenderSettings.customReflection = tpl.customReflection;
              original = None._;
            }
          }
        }
      );
    }
  }

  [PublicAPI, Record(ConstructorFlags.Constructor)]
  public readonly partial struct ReflectionMode {
    /// <summary>If this is `None`, <see cref="DefaultReflectionMode.Skybox"/> will be used.</summary>
    public readonly Option<Cubemap> maybeCustomReflection;

    public DefaultReflectionMode mode =>
      maybeCustomReflection.isSome ? DefaultReflectionMode.Custom : DefaultReflectionMode.Skybox;

    /// <summary>Use the <see cref="DefaultReflectionMode.Skybox"/>.</summary>
    public static ReflectionMode skybox => new(maybeCustomReflection: None._);

    /// <summary>Use the provided custom <see cref="Cubemap"/> for reflections.</summary>
    public static ReflectionMode custom(Cubemap reflection) => new(maybeCustomReflection: Some.a(reflection));
  } 
}