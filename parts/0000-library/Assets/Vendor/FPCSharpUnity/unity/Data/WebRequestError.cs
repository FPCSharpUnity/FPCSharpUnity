using System;
using System.Collections.Immutable;
using System.Linq;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.unity.Concurrent.unity_web_request;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Data {
  /// <summary>
  /// An error has happened while executing a <see cref="UnityWebRequest"/>.
  /// </summary>
  [PublicAPI, Union(new [] {
    typeof(UnacceptableResponseCode), typeof(NoInternetError), typeof(UserAborted), typeof(NonSuccessResult),
    typeof(SuccessHandlerFailed)
  })] public readonly partial struct WebRequestError {
    /// <summary>Turns this into <see cref="LogEntry"/>.</summary>
    public ErrorMsg simplify => this.foldM(
      onUnacceptableResponseCode: err => new ErrorMsg(err.ToString()),
      onNonSuccessResult: err => new ErrorMsg(err.ToString()),
      onSuccessHandlerFailed: err => new ErrorMsg(err.ToString()),
      onNoInternetError: err => new ErrorMsg(err.ToString(), reportToErrorTracking: false),
      onUserAborted: err => new ErrorMsg(err.ToString(), reportToErrorTracking: false)
    );
  }
  
  /// <summary>The returned response code was not in the <see cref="AcceptedResponseCodes"/>.</summary>
  [Record(ConstructorFlags.Constructor)] public readonly partial struct UnacceptableResponseCode {
    /// <summary>Url of the request.</summary>
    public readonly Url url;
    
    /// <summary>Response code of the HTTP request. See <see cref="UnityWebRequest.responseCode"/>.</summary>
    public readonly long responseCode;

    /// <summary>Response codes that we would have accepted for this request.</summary>
    public readonly AcceptedResponseCodes acceptedResponseCodes;

    /// <summary>See <see cref="UnityWebRequest.GetResponseHeaders"/>.</summary>
    public readonly ImmutableDictionary<string, string> headers;

    /// <summary>
    /// Text of the response (not available if the <see cref="UnityWebRequest.downloadHandler"/> is
    /// <see cref="DownloadHandlerAssetBundle"/> as it does not support text access).
    /// </summary>
    public readonly Option<string> responseText;

    /// <summary>Detailed message about the request for debugging.</summary>
    public string message => $"Received response code {responseCode} was not in {acceptedResponseCodes}";
    
    /// <summary>Detailed <see cref="LogEntry"/> about the request for debugging.</summary>
    public LogEntry logEntry => new LogEntry(
      message,
      extras: 
        headers.Select(kv => KV.a($"header:{kv.Key}", kv.Value))
          .Concat(responseText.mapM(text => KV.a("response-text", text)).asEnumerable())
          .toImmutableArrayC()
    );
  }
  
  /// <summary>Unity reported that we do not have an internet connection.</summary>
  [Record(ConstructorFlags.Constructor)] public readonly partial struct NoInternetError {
    /// <summary>Url of the request.</summary>
    public readonly Url url;
  }
  
  /// <summary>Unity reported that <see cref="UnityWebRequest.Abort"/> was called.</summary>
  [Record(ConstructorFlags.Constructor)] public readonly partial struct UserAborted {
    /// <summary>Url of the request.</summary>
    public readonly Url url;
  }
  
  /// <summary>
  /// <see cref="UnityWebRequest.get_result"/> did not return <see cref="UnityWebRequest.Result.Success"/>.
  /// </summary>
  [Record(ConstructorFlags.Constructor, GenerateToString = true)] public readonly partial struct NonSuccessResult {
    /// <summary>Url of the request.</summary>
    public readonly Url url;

    /// <summary>
    /// Result from <see cref="UnityWebRequest.get_result"/>. This will never be
    /// <see cref="UnityWebRequest.Result.Success"/>.
    /// </summary>
    public readonly UnityWebRequestNonSuccessfulResult result;
    
    /// <summary>Response code of the HTTP request. See <see cref="UnityWebRequest.responseCode"/>.</summary>
    public readonly long responseCode;

    /// <summary>Error from <see cref="UnityWebRequest.error"/>.</summary>
    public readonly string error;

    /// <summary>
    /// When a UnityWebRequest ends with the result, <see cref="UnityWebRequest.Result.DataProcessingError"/>, the
    /// message describing the error is in the download handler.
    /// <para/>
    /// https://docs.unity3d.com/ScriptReference/Networking.DownloadHandler-error.html
    /// </summary>
    public readonly Option<string> dataProcessingError;

    /// <summary>Contains the body text. Can be `None` if the body was not available.</summary>
    public readonly Option<string> bodyText;
  }

  /// <summary>
  /// <see cref="ASync.toFutureCancellable{A}"/> threw an exception while running the success handler.
  /// </summary>
  [Record(ConstructorFlags.Constructor)] public readonly partial struct SuccessHandlerFailed {
    /// <summary>Url of the request.</summary>
    public readonly Url url;
    
    /// <summary>Response code of the HTTP request. See <see cref="UnityWebRequest.responseCode"/>.</summary>
    public readonly long responseCode;

    /// <summary>Exception that was thrown while running <see cref="ASync.toFutureCancellable{A}"/>.</summary>
    public readonly Exception exception;
  }
}