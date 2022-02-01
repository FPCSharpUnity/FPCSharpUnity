using System;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.serialization;
using FPCSharpUnity.core.utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Data {
  [PublicAPI] public static class SerializedRWU {
    public static readonly ISerializedRW<Vector2> vector2 =
      SerializedRW.flt.and(SerializedRW.flt, (x, y) => new Vector2(x, y), _ => _.x, _ => _.y);

    public static readonly ISerializedRW<Vector3> vector3 =
      vector2.and(SerializedRW.flt, (v2, z) => new Vector3(v2.x, v2.y, z), _ => _, _ => _.z);

    public static readonly ISerializedRW<Url> url = 
      SerializedRW.str.map<string, Url>(_ => new Url(_), _ => _.url);

    public static readonly ISerializedRW<TextureFormat> textureFormat =
      SerializedRW.integer.map(
        i => 
          EnumUtils.GetValues<TextureFormat>().find(_ => (int) _ == i)
          .toRight($"Can't find texture format by {i}"),
        tf => (int) tf
      );

    public static readonly ISerializedRW<Color32> color32 =
      BytePair.rw.and(BytePair.rw, 
        (bp1, bp2) => {
          var (r, g) = bp1;
          var (b, a) = bp2;
          return new Color32(r, g, b, a);
        },
        c => new BytePair(c.r, c.g),
        c => new BytePair(c.b, c.a)
      );
    
    // RWs for library or user defined types go as static fields of those types. 

#if UNITY_EDITOR
    [PublicAPI] public static ISerializedRW<A> unityObjectSerializedRW<A>() where A : Object =>
      PathStr.serializedRW.map<PathStr, A>(
        path => {
          try {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<A>(path);
          }
          catch (Exception e) {
            return $"loading {typeof(A).FullName} from '{path}' threw {e}";
          }
        },
        module => module.editorAssetPath()
      );
#endif
    
    [LazyProperty] public static ISerializedRW<BatteryStatus> batteryStatus =>
      SerializedRW.byte_.mapNoFail(b => (BatteryStatus) b, s => ((int) s).toByteClamped());
  }
}