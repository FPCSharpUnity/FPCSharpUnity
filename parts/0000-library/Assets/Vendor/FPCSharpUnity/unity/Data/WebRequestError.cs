using FPCSharpUnity.core.log;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Data {
  [Record]
  public partial struct WebRequestError {
    [PublicAPI] public readonly Url url;
    [PublicAPI] public readonly Either<LogEntry, NoInternetError> message;

    [PublicAPI] public LogEntry simplify => message.fold(
      err => err, 
      nie => new ErrorMsg($"No internet: {nie.message}", reportToErrorTracking: false)
    );
  }
  
  [Record]
  public partial struct NoInternetError {
    [PublicAPI] public readonly string message;
  }
}
