using ExhaustiveMatching;
using FPCSharpUnity.core.functional;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Data; 

/// <summary>
/// As <see cref="UnityWebRequest.Result"/>, however does not include the <see cref="UnityWebRequest.Result.Success"/>
/// case.
/// </summary>
public enum UnityWebRequestNonSuccessfulResult {
  /// <inheritdoc cref="UnityWebRequest.Result.InProgress"/>
  InProgress = UnityWebRequest.Result.InProgress,
    
  /// <inheritdoc cref="UnityWebRequest.Result.ConnectionError"/>
  ConnectionError = UnityWebRequest.Result.ConnectionError,
    
  /// <inheritdoc cref="UnityWebRequest.Result.ProtocolError"/>
  ProtocolError = UnityWebRequest.Result.ProtocolError,
    
  /// <inheritdoc cref="UnityWebRequest.Result.DataProcessingError"/>
  DataProcessingError = UnityWebRequest.Result.DataProcessingError
}

public static class UnityWebRequestNonSuccessfulResultExts {
  /// <summary>
  /// Returns `Some` if the <see cref="UnityWebRequest.Result"/> is not <see cref="UnityWebRequest.Result.Success"/>.
  /// </summary>
  public static Option<UnityWebRequestNonSuccessfulResult> toNonSuccessfulResult(this UnityWebRequest.Result r) =>
    r switch {
      UnityWebRequest.Result.Success => None._,
      UnityWebRequest.Result.InProgress => Some.a(UnityWebRequestNonSuccessfulResult.InProgress),
      UnityWebRequest.Result.ConnectionError => Some.a(UnityWebRequestNonSuccessfulResult.ConnectionError),
      UnityWebRequest.Result.ProtocolError => Some.a(UnityWebRequestNonSuccessfulResult.ProtocolError),
      UnityWebRequest.Result.DataProcessingError => Some.a(UnityWebRequestNonSuccessfulResult.DataProcessingError),
      _ => throw ExhaustiveMatch.Failed(r)
    };
}