﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using FPCSharpUnity.unity.caching;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Filesystem {
  public class FilesystemDataCache : ICache<byte[]> {
    [PublicAPI]
    public static Option<FilesystemDataCache> forPath(string pathStr) =>
      pathStr.nonEmptyOpt(trim: true).map(path =>
        new FilesystemDataCache(new PathStr(path))
      );

    /// <summary>
    /// Sometimes persistent data path is null: https://www.google.lt/search?q=unity+persistentDataPath+null
    /// 
    /// Lazy because we can't access unity API in static class constructors.
    /// </summary>
    [PublicAPI]
    public static LazyVal<Option<FilesystemDataCache>> persistent = 
      F.lazy(() => forPath(Application.persistentDataPath));
    
    /// <summary>
    /// Same thing with temporary cache path.
    /// </summary>
    [PublicAPI]
    public static LazyVal<Option<FilesystemDataCache>> temporary = 
      F.lazy(() => forPath(Application.temporaryCachePath));

    public readonly PathStr root;

    FilesystemDataCache(PathStr root) { this.root = root; }

    public ICachedBlob<byte[]> blobFor(string name) => fileBlobFor(name);
    [PublicAPI] public FileCachedBlob fileBlobFor(string name) => new FileCachedBlob(root / name);

    public Try<IEnumerable<PathStr>> files => 
      F.doTry(() => Directory.GetFiles(root).Select(_ => new PathStr(_)));

    [PublicAPI]
    public Try<FilesystemDataCache> scoped(string scope) => F.doTry(() => {
      var newPath = root / scope;
      Directory.CreateDirectory(newPath);
      return new FilesystemDataCache(newPath);
    });
  }

  public static class FilesystemDataCacheExts {
    [PublicAPI] public static ICache<byte[]> asCache(this FilesystemDataCache cache) => cache;
  }
}
