#if WWW_ENABLED
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;
// obsolete WWW
#pragma warning disable 618

namespace FPCSharpUnity.unity.Extensions {
  public static class WWWExts {
    public static Either<WWWError, WWW> toEither(this WWW www) =>
      string.IsNullOrEmpty(www.error)
      ? F.right<WWWError, WWW>(www)
      : F.left<WWWError, WWW>(new WWWError(www));

    public static Either<WWWError, Texture2D> asTexture(this Either<WWWError, WWW> either) =>
      either.flatMapRightM(www => {
        // NonReadable textures take 2x less ram
        var tex = www.textureNonReadable;
        return tex
          ? Either<WWWError, Texture2D>.Right(tex)
          : Either<WWWError, Texture2D>.Left(new WWWError(www, "WWW didn't produce a texture!"));
      });

    public static WWWWithHeaders headers(this WWW www) =>
      new WWWWithHeaders(www, www.responseHeaders.ToImmutableDictionary());

    public static void trackWWWSend(this WWW www, string prefix, Dictionary<string, string> headers) {
      ASync.StartCoroutine(ASync.WWWEnumerator(www).afterThis(() => {
        if (!string.IsNullOrEmpty(www.error)) {
          if (Log.d.isInfo()) Log.d.info(
            $"{prefix} send to '{www.url}' failed with: {www.error}" +
            "\nRequest headers=" + headers.asDebugString() +
            "\nResponse headers=" + www.responseHeaders.asDebugString()
          );
        }
        else {
          if (Debug.isDebugBuild && Log.d.isInfo()) Log.d.info(
            prefix + " send succeeded with response headers=" + www.responseHeaders.asDebugString()
          );
        }
      }));
    }

    public static void trackWWWSend(this WWW www, string prefix, WWWForm form) {
      ASync.StartCoroutine(ASync.WWWEnumerator(www).afterThis(() => {
        if (!string.IsNullOrEmpty(www.error)) {
          if (Log.d.isInfo()) Log.d.info(
            $"{prefix} POST send to '{www.url}' failed with: {www.error}" +
            "\nRequest headers=" + form.headers.asDebugString() +
            "\nRequest body=" + Encoding.UTF8.GetString(form.data) +
            "\nResponse headers=" + www.responseHeaders.asDebugString()
          );
        }
        else {
          if (Debug.isDebugBuild && Log.d.isInfo()) Log.d.info(
            prefix + " POST send succeeded with response headers=" + www.responseHeaders.asDebugString()
          );
        }
      }));
    }
  }

  /**
   * Struct that wraps the parsed headers, because `www.responseHeaders` parses headers
   * each time it is called.
   **/
  public struct WWWWithHeaders {
    public readonly WWW www;
    public readonly IReadOnlyDictionary<string, string> headers;

    public WWWWithHeaders(WWW www, IReadOnlyDictionary<string, string> headers) {
      this.www = www;
      this.headers = headers;
    }

    public Option<string> this[string key] => headers.get_(key);
  }
}
#endif