﻿using System.IO;
using FPCSharpUnity.unity.caching;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.serialization;


namespace FPCSharpUnity.unity.Filesystem {
  [PublicAPI] public class FileCachedBlob : ICachedBlob<byte[]> {
    public readonly PathStr path;

    public FileCachedBlob(PathStr path) { this.path = path; }

    public override string ToString() => $"{nameof(FileCachedBlob)}[{path}]";

    public bool cached => File.Exists(path);
    public Option<Try<byte[]>> read() => read(path);
    public Try<Unit> store(byte[] data) => store(path, data);
    public Try<Unit> clear() => clear(path);
    
    public static Option<Try<byte[]>> read(PathStr path) =>
      File.Exists(path)
        ? F.doTry(() => File.ReadAllBytes(path)).some()
        : F.none<Try<byte[]>>();
    
    public static Try<Unit> store(PathStr path, byte[] data) => 
      F.doTry(() => File.WriteAllBytes(path, data));
    
    public static Try<Unit> clear(PathStr path) => F.doTry(() => File.Delete(path));

    public static ICachedBlob<A> a<A>(
      ISerializedRW<A> rw, PathStr path, A defaultValue, 
      ILog log = null, LogLevel onDeserializeFailureLogLevel = LogLevel.ERROR
    ) {
      log ??= Log.d;
      var stream = new MemoryStream();
      return new FileCachedBlob(path).bimap(BiMapper.a(
        (byte[] bytes) => {
          var deserializedEither = rw.deserialize(bytes, 0);
          if (deserializedEither.leftValueOut(out var err)) {
            if (log.willLog(onDeserializeFailureLogLevel))
              log.log(
                onDeserializeFailureLogLevel, 
                $"Can't deserialize {path} because of {err}, deleting and returning default value."
              );
            clear(path).getOrLog($"Couldn't clear file: '{path}'", log: log);
            return defaultValue;
          }
          return deserializedEither.__unsafeGetRight.value;
        },
        a => rw.serializeToArray(a, stream)
      ));
    }
  }
}