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

    /// <summary>
    /// Checks <see cref="ParticleSystem"/>'s <see cref="ParticleSystem.isPlaying"/> before calling
    /// <see cref="ParticleSystem.Play()"/> or <see cref="ParticleSystem.Stop()"/>
    /// Only plays the system if it is not currently playing.
    /// Only stops the system if it is currently playing.
    /// </summary>
    public static void setPlaying(this ParticleSystem particleSystem, bool playing, bool withChildren = true) {
      if (playing) {
        if (!particleSystem.isPlaying) {
          particleSystem.Play(withChildren: withChildren);
        }
      }
      else {
        if (particleSystem.isPlaying) {
          particleSystem.Stop(withChildren: withChildren);
        }
      }
    }

    /// <summary>
    /// <see cref="ParticleSystemExts.setPlaying(ParticleSystem, bool, bool)"/>
    /// </summary>
    public static void setPlaying(this ParticleSystem[] particleSystems, bool playing, bool withChildren = true) {
      foreach (var particleSystem in particleSystems) {
        particleSystem.setPlaying(playing: playing, withChildren: withChildren);
      }
    }
  }
}
