using System;
using FPCSharpUnity.core.exts;
using JetBrains.Annotations;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI]
  public static class ParticleSystemExts {
    public static void restart(this ParticleSystem ps) {
      ps.time = 0;
      ps.Play();
    }
    
    public static void setEmissionEnabled(this ParticleSystem particleSystem, bool enabled) {
      var emmission = particleSystem.emission;
      emmission.enabled = enabled;
    }

    public static void playWithoutChildren(this ParticleSystem[] array) {
      foreach (var ps in array) {
        ps.Play(withChildren: false);
      }
    }

    public static void stopWithoutChildren(this ParticleSystem[] array, ParticleSystemStopBehavior stopBehavior) {
      foreach (var ps in array) {
        ps.Stop(withChildren: false, stopBehavior);
      }
    }
    
    public static void setColorOverLifeTime(this ParticleSystem system, ParticleSystem.MinMaxGradient gradient) {
      var colorOverLifeTime = system.colorOverLifetime;
      colorOverLifeTime.color = gradient;
    }
    
    public static void setStartColor(this ParticleSystem system, Color color) {
      var main = system.main;
      main.startColor = color;
    }

    /// <summary>Actions for controlling <see cref="ParticleSystem"/>s 'Play' and 'Stop'.</summary>
    public enum Control : byte {
      /// <summary>Plays particle system with children, <see cref="ParticleSystem.Play()"/>.</summary>
      Play, 
      /// <summary>Stops particle system with children, <see cref="ParticleSystem.Stop()"/>.</summary>
      Stop
    }

    public static Control toParticleSystemControl(this bool enabled) => enabled ? Control.Play : Control.Stop;

    /// <summary>
    /// Use this when you want to avoid multiple <see cref="ParticleSystem.Play()"/> or <see cref="ParticleSystem.Stop()"/>
    /// calls on update, for this '<see cref="particleSystem"/>'.
    /// </summary>
    public static void handleOnUpdate(this ParticleSystem particleSystem, Control control) {
      switch (control) {
        case Control.Play:
          if (!particleSystem.isPlaying) {
            particleSystem.Play();
          }
          break;
        case Control.Stop:
          if (particleSystem.isPlaying) {
            particleSystem.Stop();
          }
          break;
        default:
          throw control.argumentOutOfRange();
      }
    }
  }
}
