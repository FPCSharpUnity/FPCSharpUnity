using FPCSharpUnity.unity.Data;
using JetBrains.Annotations;
using FPCSharpUnity.core.config;
using FPCSharpUnity.core.functional;
using static FPCSharpUnity.unity.Configuration.ConfigU;

namespace FPCSharpUnity.unity.Configuration; 

[PublicAPI] public static class IConfigExtsU {
  public static Range getIRange(this IConfig cfg, string key) => cfg.get(key, iRangeParser);
  public static FRange getFRange(this IConfig cfg, string key) => cfg.get(key, fRangeParser);
  public static URange getURange(this IConfig cfg, string key) => cfg.get(key, uRangeParser);
  public static Duration getDuration(this IConfig cfg, string key) => cfg.get(key, Duration.configParser);
    
  public static Option<Range> optIRange(this IConfig cfg, string key) => cfg.optGet(key, iRangeParser);
  public static Option<FRange> optFRange(this IConfig cfg, string key) => cfg.optGet(key, fRangeParser);
  public static Option<URange> optURange(this IConfig cfg, string key) => cfg.optGet(key, uRangeParser);
    
  public static Either<ConfigLookupError, Range> eitherIRange(this IConfig cfg, string key) => cfg.eitherGet(key, iRangeParser);
  public static Either<ConfigLookupError, FRange> eitherFRange(this IConfig cfg, string key) => cfg.eitherGet(key, fRangeParser);
  public static Either<ConfigLookupError, URange> eitherURange(this IConfig cfg, string key) => cfg.eitherGet(key, uRangeParser);
}