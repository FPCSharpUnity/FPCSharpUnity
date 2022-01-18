﻿using System;
using FPCSharpUnity.unity.Functional;
using UnityEngine;
using UnityEngine.UI;

namespace FPCSharpUnity.unity.Extensions {
  public static class SpriteExts {
    public static void replaceSpriteTexture<A>(
      this A a, Texture2D texture,
      Func<A, Sprite> getSprite, Action<A, Sprite> setSprite
    ) {
      // Might not have a sprite there
      var origOpt = F.opt(getSprite(a));
      Sprite sprite;
      var rect = new Rect(0, 0, texture.width, texture.height);

      if (origOpt.isSome) {
        var orig = origOpt.get;
        var pivot = new Vector2(
          orig.pivot.x / orig.rect.width, orig.pivot.y / orig.rect.height
        );
        var pixelsPerUnit = orig.pixelsPerUnit * (texture.width / orig.rect.width);
        sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
      }
      else {
        sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
      }
      setSprite(a, sprite);
    }

    public static void replaceSpriteTexture(this SpriteRenderer r, Texture2D texture) =>
      r.replaceSpriteTexture(texture, _ => _.sprite, (_, s) => _.sprite = s);

    public static void replaceSpriteTexture(this Image r, Texture2D texture) =>
      r.replaceSpriteTexture(texture, _ => _.sprite, (_, s) => _.sprite = s);
  }
}