using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.unity.Concurrent.unity_web_request;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Extensions {
  public static class UnityWebRequestExts {
    [PublicAPI]
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

    [PublicAPI]
    public static Future<Either<LogEntry, byte[]>> downloadToRamSimpleError(
      this UnityWebRequest req, AcceptedResponseCodes acceptedResponseCodes
    ) => req.downloadToRam(acceptedResponseCodes).map(_ => _.mapLeft(err => err.simplify));
  }
}