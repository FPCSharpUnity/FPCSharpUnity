﻿using System.Collections;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Components {
  public class TweenColorOnEnable : MonoBehaviour, IMB_Awake, IMB_OnEnable {
    public Color targetColor;
    public float tweenInDuration, delayDuration, tweenOutDuration;

    public void Awake() { enabled = false; }

    public void OnEnable() { StartCoroutine(flicker()); }

    IEnumerator flicker() {
      var sprites = gameObject.GetComponentsInChildren<SpriteRenderer>();
      if (sprites.nonEmpty()) {
        var originalColors = sprites.map(_ => _.color);
        foreach (var p in new CoroutineInterval(tweenInDuration.seconds())) {
          setColors(sprites, originalColors, p.value);
          yield return null;
        }
        setColors(sprites, originalColors, 1);
        yield return new WaitForSeconds(delayDuration);
        foreach (var p in new CoroutineInterval(tweenOutDuration.seconds())) {
          setColors(sprites, originalColors, 1 - p.value);
          yield return null;
        }
        setColors(sprites, originalColors, 0);
      }
      enabled = false;
    }

    void setColors(SpriteRenderer[] sprites, Color[] colors, float ratio) {
      for (var i = 0; i < sprites.Length; i++) {
        sprites[i].color = Color.Lerp(colors[i], targetColor, ratio);
      }
    }
  }
}