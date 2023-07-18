using System.Linq;
using FPCSharpUnity.core.collection;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using UnityEngine.Networking;

namespace FPCSharpUnity.unity.Concurrent.unity_web_request; 

/// <summary>
/// A list of accepted response codes for a <see cref="UnityWebRequest"/>.
/// <para/>
/// See also: https://en.wikipedia.org/wiki/List_of_HTTP_status_codes
/// </summary>
[Record(GenerateToString = false), PublicAPI]
public sealed partial class AcceptedResponseCodes {
  public readonly ImmutableArrayC<long> codes;

  public bool contains(long responseCode) =>
    codes._unsafeArray.Contains(responseCode);

  public static AcceptedResponseCodes operator +(AcceptedResponseCodes c1, AcceptedResponseCodes c2) =>
    new(c1.codes._unsafeArray.concat(c2.codes._unsafeArray).toImmutableArrayCMove());

  public override string ToString() =>
    $"{nameof(AcceptedResponseCodes)}[{codes.mkStringEnum(", ", "", "")}]";
    
  public static readonly AcceptedResponseCodes
    _20X = new AcceptedResponseCodes(ImmutableArrayC.create(
      200L, 201, 202, 203, 204, 205, 206, 207, 208, 226
    )),
    _404 = new AcceptedResponseCodes(ImmutableArrayC.create(404L));
}