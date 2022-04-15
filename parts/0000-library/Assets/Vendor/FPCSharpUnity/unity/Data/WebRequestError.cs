using FPCSharpUnity.core.log;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Data {
  /// <summary>
  /// An error has happened while executing a <see cref="UnityWebRequest"/>.
  /// </summary>
  [Record, PublicAPI] public readonly partial struct WebRequestError {
    public readonly Url url;
    public readonly OneOf<LogEntry, NoInternetError, UserAborted> message;

    /// <summary>Turns this into <see cref="LogEntry"/>.</summary>
    public LogEntry simplify => message.fold(
      err => err, 
      nie => new ErrorMsg($"No internet: {nie.message}", reportToErrorTracking: false),
      userAborted => new ErrorMsg($"User aborted: {userAborted.message}", reportToErrorTracking: false)
    );
  }
  
  /// <summary>Unity reported that we do not have an internet connection.</summary>
  [Record(ConstructorFlags.Constructor)] public readonly partial struct NoInternetError {
    /// <summary>Detailed message about the request for debugging.</summary>
    public readonly string message;
  }
  
  /// <summary>Unity reported that <see cref="UnityWebRequest.Abort"/> was called.</summary>
  [Record(ConstructorFlags.Constructor)] public readonly partial struct UserAborted {
    /// <summary>Detailed message about the request for debugging.</summary>
    public readonly string message;
  }
}
