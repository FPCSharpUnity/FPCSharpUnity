using System;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Concurrent.unity_web_request;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Extensions; 

[PublicAPI]
public static class UnityWebRequestExts {
  public static Future<Either<WebRequestError, byte[]>> downloadToRam(
    this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes
  ) {
    var handler = 
      req.downloadHandler is DownloadHandlerBuffer h 
        ? h 
        : new DownloadHandlerBuffer();
    req.downloadHandler = handler;
    return req.toFuture(acceptedResponseCodes, _ => handler.data);
  }

  public static Future<Either<WebRequestError, string>> downloadToRamText(
    this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes
  ) => req.toFuture(acceptedResponseCodes, static _ => _.downloadHandler.text);

  public static Future<Either<ErrorMsg, byte[]>> downloadToRamSimpleError(
    this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes
  ) => req.downloadToRam(acceptedResponseCodes).map(_ => _.mapLeftM(err => err.simplify));

  /// <param name="uri"></param>
  /// <param name="acceptedResponseCodes"></param>
  /// <param name="readable">
  /// false - less RAM usage<br/>
  /// true - allows you to read pixel data on the CPU
  /// </param>
  /// <returns></returns>
  public static ICancellableFuture<Either<WebRequestError, Texture2D>> downloadTextureToRam(
    this Uri uri, AcceptedResponseCodes acceptedResponseCodes, bool readable = false
  ) {
    var downloadHandlerTexture = new DownloadHandlerTexture(readable: readable); 
    var req = new UnityWebRequest(
      uri, UnityWebRequest.kHttpVerbGET, downloadHandler: downloadHandlerTexture, uploadHandler: null
    );
    return req.toFutureCancellable(acceptedResponseCodes, _ => downloadHandlerTexture.texture);
  }

  /// <summary>Typesafe version of <see cref="UnityWebRequest.GetRequestHeader"/>.</summary>
  public static Option<string> GetRequestHeaderSafe(this UnityWebRequest req, string headerName) =>
    // From Unity docs:
    //  If no custom header with a matching name has been set, returns an empty string.
    //
    // We check for `null` as well just in case.
    req.GetRequestHeader(headerName).notNullOrEmptyOpt();

  /// <summary>
  /// Typesafe version of <see cref="UnityWebRequest.GetRequestHeader"/> returning <see cref="Either{A,B}"/>.
  /// </summary>
  public static Either<string, string> GetRequestHeaderSafeE(this UnityWebRequest req, string headerName) =>
    req.GetRequestHeaderSafe(headerName).toRightM(() => $"No request header '{s(headerName)}'");

  /// <summary>Typesafe version of <see cref="UnityWebRequest.GetResponseHeader"/>.</summary>
  public static Option<string> GetResponseHeaderSafe(this UnityWebRequest req, string headerName) =>
    // From Unity docs:
    //   If no header with a matching name has been received, or no responses have been received, returns null.
    Option.a(req.GetResponseHeader(headerName));

  /// <summary>
  /// Typesafe version of <see cref="UnityWebRequest.GetResponseHeader"/> returning <see cref="Either{A,B}"/>.
  /// </summary>
  public static Either<string, string> GetResponseHeaderSafeE(this UnityWebRequest req, string headerName) =>
    req.GetResponseHeaderSafe(headerName).toRightM(() => $"No response header '{s(headerName)}'");
  
  /// <summary>
  /// Typesafe version of <see cref="UnityWebRequest.GetResponseHeader"/> returning <see cref="Either{A,B}"/> which
  /// parses the raw string value.
  /// </summary>
  public static Either<string, A> GetResponseHeaderSafeE<A>(
    this UnityWebRequest req, string headerName, Func<string, Either<string, A>> parser
  ) => req.GetResponseHeaderSafeE(headerName).flatMapRightM(str => 
    parser(str).mapLeftM(err => $"Can't parse header '{s(headerName)}': {s(err)}")
  );
}