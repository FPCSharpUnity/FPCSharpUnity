using System;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.config;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.json;
using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.Data {
  [Serializable, Record, PublicAPI] public partial struct DurationRange : IStr {
    [SerializeField, PublicAccessor] Duration _from, _to;

    public string asString() => $"[{_from}..{_to}]";

    public Duration random(ref Rng rng) => 
      new Duration(GenRanged.Int.next(ref rng, _from.millis, _to.millis));

    public static readonly Config.Parser<JsonValue, DurationRange> parser =
      Config.immutableArrayParser(Duration.configParser).flatMap((_, cfg) => {
        if (cfg.Length == 2) 
          return Either<ConfigLookupError, DurationRange>.Right(new DurationRange(cfg[0], cfg[1]));
        else
          return ConfigLookupError.fromException(new Exception(
            $"Expected duration range to have 2 elements, but it had {cfg.Length}"
          ));
      });
  }
}