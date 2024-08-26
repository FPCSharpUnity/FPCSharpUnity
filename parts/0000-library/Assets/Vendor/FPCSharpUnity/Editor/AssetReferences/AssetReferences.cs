using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExhaustiveMatching;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.macros;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Editor.extensions;
using UnityEditor;
using static FPCSharpUnity.core.typeclasses.Str;
using ImmutableList = System.Collections.Immutable.ImmutableList;

namespace FPCSharpUnity.unity.Editor.AssetReferences {
  public partial class AssetReferences {
    /// <summary>Amount of workers (CPU threads) used.</summary>
    public readonly int workers;

    /// <summary> Whether to use default resolver that parses direct Unity references. </summary>
    public readonly bool useDefaultResolver;
    
    /// <summary> Extra resolvers are project specific reference resolvers that resolve links between assets. </summary>
    public readonly ImmutableArrayC<BytesParserAndGuidResolver> extraResolvers;
    
    public readonly Parents parents = new();
    public readonly Children children = new();
    readonly Dictionary<AssetPath, AssetGuid> pathToGuid = new();
    public string scanDuration = "";

    AssetReferences(int workers, bool useDefaultResolver, ImmutableArrayC<BytesParserAndGuidResolver> extraResolvers) {
      this.workers = workers;
      this.useDefaultResolver = useDefaultResolver;
      this.extraResolvers = extraResolvers;
    }

    /// <param name="data"></param>
    /// <param name="progress"></param>
    /// <param name="log"></param>
    /// <param name="useDefaultResolver">See <see cref="useDefaultResolver"/></param>
    /// <param name="extraResolvers">See <see cref="extraResolvers"/></param>
    public static AssetReferences a(
      AssetUpdate data, Ref<float> progress, ILog log, bool useDefaultResolver, 
      ImmutableArrayC<BytesParserAndGuidResolver> extraResolvers
    ) {
      // It runs slower if we use too many threads ¯\_(ツ)_/¯
      var workers = Math.Min(Environment.ProcessorCount, 10);
      
      var refs = new AssetReferences(workers, useDefaultResolver, extraResolvers);
      var sw = Stopwatch.StartNew();
      process(
        data, workers,
        pathToGuid: refs.pathToGuid, parents: refs.parents, children: refs.children,
        progress: progress, log: log, useDefaultResolver: refs.useDefaultResolver, extraResolvers: refs.extraResolvers
      );
      refs.scanDuration = s(sw.Elapsed);
      return refs;
    }

    public static readonly Regex
      // GUID regex for matching guids in meta files
      metaGuid = new Regex(@"guid: (\w+)", RegexOptions.Compiled),
      // GUID regex for matching guids in other files
      guidRegex = new Regex(
        @"{fileID:\s+(\d+),\s+guid:\s+(\w+),\s+type:\s+\d+}",
        RegexOptions.Compiled | RegexOptions.Multiline
      );

    public void update(AssetUpdate data, Ref<float> progress, ILog log) {
      process(
        data, workers,
        pathToGuid: pathToGuid, parents: parents, children: children,
        progress: progress, log: log, useDefaultResolver: useDefaultResolver, extraResolvers: extraResolvers
      );
    }

    /// <summary>
    /// Given an object GUID find all scenes where that particular GUID is being used.
    /// </summary>
    /// <returns>guids for scenes where given guid is used</returns>
    public System.Collections.Immutable.ImmutableList<Chain> findParentScenes(ReadOnlySpan<AssetGuid> guids) => 
      findDependencyChains(guids, ChildOrParent.Parent, path => path.path.EndsWithFast(".unity"));
    
    /// <summary>
    /// Given an object GUID find all resources where that particular GUID is being used.
    /// </summary>
    /// <returns>guids for resources where given guid is used</returns>
    public System.Collections.Immutable.ImmutableList<Chain> findParentResources(ReadOnlySpan<AssetGuid> guids) => 
      findDependencyChains(guids, ChildOrParent.Parent, path => path.path.ToLowerInvariant().Contains("/resources/"));

    /// <summary>
    /// Given an object GUID find all children that are referenced by this object, either directly or indirectly.
    /// </summary>
    /// <returns>Guids of children assets, including the current asset passed as a parameter.</returns>
    public ImmutableArray<AssetGuid> findAllChildren(AssetGuid guid, Func<AssetPath, bool> shouldConsiderPath) {
      var visited = new HashSet<string>();
      var q = new Queue<AssetGuid>();
      q.Enqueue(guid);
      var res = ImmutableArray.CreateBuilder<AssetGuid>();
      while (q.Count > 0) {
        var currentGuid = q.Dequeue();
        if (visited.Contains(currentGuid)) continue;
        visited.Add(currentGuid);
        res.Add(currentGuid);
        var path = new AssetPath(AssetDatabase.GUIDToAssetPath(currentGuid));
        if (!shouldConsiderPath(path)) continue;
        if (children.children.TryGetValue(currentGuid, out var currentChildren)) {
          foreach (var child in currentChildren.guidsInFile.Keys) {
            if (!visited.Contains(child)) q.Enqueue(child);
          }
        }
      }
      return res.ToImmutable();
    }
    
    public ImmutableArray<AssetGuid> findAllChildren(AssetGuid[] guid) {
      var visited = new HashSet<string>();
      var q = new Queue<AssetGuid>();
      foreach (var g in guid) q.Enqueue(g);
      var res = ImmutableArray.CreateBuilder<AssetGuid>();
      while (q.Count > 0) {
        var currentGuid = q.Dequeue();
        if (visited.Contains(currentGuid)) continue;
        visited.Add(currentGuid);
        res.Add(currentGuid);
        if (children.children.TryGetValue(currentGuid, out var currentChildren)) {
          foreach (var child in currentChildren.guidsInFile.Keys) {
            if (!visited.Contains(child)) q.Enqueue(child);
          }
        }
      }
      return res.ToImmutable();
    }

    [Record] public sealed partial class Chain {
      public readonly NonEmpty<System.Collections.Immutable.ImmutableList<AssetGuid>> guids;

      public AssetGuid mainGuid => guids.head();
    }
    
    public enum ChildOrParent {
      Child, Parent
    }
    
    /// <param name="guid"></param>
    /// <param name="childOrParent">
    /// which direction the search should go? Search for children or parents?
    /// </param>
    /// <param name="pathPredicate"></param>
    /// <returns></returns>
    public System.Collections.Immutable.ImmutableList<Chain> findDependencyChains(
      ReadOnlySpan<AssetGuid> guids, ChildOrParent childOrParent, Func<AssetPath, bool> pathPredicate
    ) {
      // TODO: expensive operation. Need to cache results
      
      Neighbors neighbors = childOrParent switch {
        ChildOrParent.Child => children,
        ChildOrParent.Parent => parents,
        _ => throw ExhaustiveMatch.Failed(childOrParent)
      };
      
      // Dijkstra algorithm
      
      // guid -> child guid
      var visited = new Dictionary<AssetGuid, Option<AssetGuid>>();
      
      var q = new Queue<(AssetGuid current, Option<AssetGuid> previous)>();
      foreach (var guid in guids) {
        q.Enqueue((guid, None._));
      }
      var res = ImmutableList.CreateBuilder<Chain>();
      while (q.Count > 0) {
        var (current, maybePrevious) = q.Dequeue();
        if (visited.ContainsKey(current)) continue;
        visited.Add(current, maybePrevious);
        var path = new AssetPath(AssetDatabase.GUIDToAssetPath(current));
        if (pathPredicate(path)) {
          res.Add(makeChain(current));
        }
        neighbors.getNeighbors(current).ifSomeM(currentNeighbors => {
          foreach (var neighbor in currentNeighbors) {
            if (!visited.ContainsKey(neighbor)) q.Enqueue((neighbor, Some.a(current)));
          }
        });
      }
      return res.ToImmutable();

      Chain makeChain(AssetGuid g) {
        // head points to parent, last points to the object from which we started the search
        var builder = ImmutableList.CreateBuilder<AssetGuid>();
        builder.Add(g);
        var current = g;
        while (visited.TryGetValue(current, out var maybeChild) && maybeChild.valueOut(out var child)) {
          builder.Add(child);
          current = child;
        }

        return new Chain(builder.ToImmutable().toNonEmpty().get);
      }
    }

    static void process(
      AssetUpdate data, int workers,
      Dictionary<AssetPath, AssetGuid> pathToGuid,
      Parents parents,
      Children children,
      Ref<float> progress, ILog log,
      bool useDefaultResolver,
      ImmutableArrayC<BytesParserAndGuidResolver> extraResolvers
    ) {
      progress.value = 0;
      bool predicate(AssetPath p) {
        // Sometimes images have uppercase extensions.
        var lower = p.path.ToLowerInvariant();
        return lower.EndsWithFast(".asset")
               || lower.EndsWithFast(".prefab")
               || lower.EndsWithFast(".unity")
               || lower.EndsWithFast(".mat")
               || lower.EndsWithFast(".spriteatlas")
               || lower.EndsWithFast(".spriteatlasv2")
               || SpriteAtlasExts.isPathAnImage(lower);
      }
      
      var assets = data.filter(predicate, _ => predicate(_.fromPath));
      var firstScan = children.children.isEmpty() && parents.parents.isEmpty();
      var updatedChildren =
        firstScan
        ? children
        : new Children();

      var progressIdx = 0;
      void updateProgress() => progress.value = (float) progressIdx / assets.totalAssets;

      Parallel.ForEach(
        assets.newAssets, 
        parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = workers },
        localInit: () => new List<ParseFileResult>(), 
        body: (added, loopState, results) => {
          if (
            parseFileOptimized(
              log, added, useDefaultResolver: useDefaultResolver, extraResolvers: extraResolvers
            ).valueOut(out var parseResult)
          ) {
            results.Add(parseResult);
          }
          // parseFile(pathToGuid, log, added, updatedChildren);
          lock (data) {
            progressIdx++;
            updateProgress();
          }
          return results;
        },
        localFinally: results => {
          lock (updatedChildren) {
            foreach (var result in results) {
              pathToGuid[result.assetPath] = result.assetGuid;
              {if (updatedChildren.children.TryGetValue(result.assetGuid, out var existing)) {
                // TODO: error
                // existing.AddRange(result.childGuids);
              } else {
                updatedChildren.children.Add(result.assetGuid, result.childGuids);
              }}
            }
          }
        }
      );

      foreach (var deleted in assets.deletedAssets) {
        updateProgress();
        if (pathToGuid.ContainsKey(deleted)) {
          updatedChildren.children.getOrUpdate(pathToGuid[deleted], () => new GuidsInFile());
          pathToGuid.Remove(deleted);
        }
        progressIdx++;
      }
      foreach (var moved in assets.movedAssets) {
        updateProgress();
        foreach (var guid in pathToGuid.getAndRemove(moved.fromPath))
          pathToGuid[moved.toPath] = guid;
        progressIdx++;
      }
      updateProgress();

      if (!firstScan) {
        foreach (var parent in updatedChildren.children.Keys) {
          if (children.children.ContainsKey(parent)) {
            var dict = children.children[parent];
            foreach (var child in dict.guidsInFile.Keys) parents.parents[child].Remove(parent);
            children.children.Remove(parent);
          }
        }
      }
      addParents(parents, updatedChildren);
      if (!firstScan) {
        foreach (var kv in updatedChildren.children) {
          if (kv.Value.guidsInFile.Count > 0) children.children.Add(kv.Key, kv.Value);
        }
      }
    }

    static readonly byte[] STRING_FILE_ID = stringToAsciiBytes("{fileID:");
    static readonly byte[] STRING_GUID = stringToAsciiBytes("guid:");
    static byte[] stringToAsciiBytes(string str) => str.Select(_ => (byte) _).ToArray();
    
    static ThreadLocal<byte[]> parseFileOptimized_bufferCache = new(Array.Empty<byte>);
    
    
    /// <summary>
    /// This lets you attach extra resolvers that get used in <see cref="parseFileOptimized"/>. Each resolver can provide
    /// additional logic that collects Unity Assets to include for reference checking.
    /// <para/>
    /// This solution lets to keep your project specific code separate from this library code.
    /// </summary>
    public static readonly List<BytesParserAndGuidResolver> extraResolversForProject = new();
    
    /// <summary> See for more info <see cref="extraResolvers"/>. </summary>
    public abstract class BytesParserAndGuidResolver {
      /// <summary>
      /// Initialize parser before starting to process all files in parallel. This is useful when you have code which
      /// should be ran once and on main Unity thread only.
      /// </summary>
      public abstract void initBeforeParsing();
      
      /// <summary>
      /// Parse file bytes and return unity asset guids for <see cref="AssetReferences"/> to add to the inspection window.
      /// <para/>
      /// This function is called in parallel from multiple threads.
      /// </summary>
      public abstract ImmutableArrayC<AssetGuid> parseBuffer(byte[] buffer, int length);
    }
    
    
    /// <summary>
    /// Optimized version of <see cref="parseFileUnoptimized"/>. This works at least 10x faster.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="assetPath"></param>
    /// <param name="useDefaultResolver"> See <see cref="AssetReferences.useDefaultResolver"/> </param>
    /// <param name="extraResolvers"> See <see cref="AssetReferences.extraResolvers"/> </param>
    static Option<ParseFileResult> parseFileOptimized(
      ILog log, AssetPath assetPath, bool useDefaultResolver, ImmutableArrayC<BytesParserAndGuidResolver> extraResolvers
    ) {
      if (!getGuid(assetPath, out var guid, out var metaFileContentsBuffer, log)) return None._;

      var guidsInFile = new GuidsInFile();

      if (SpriteAtlasExts.isPathAnImage(assetPath.path.ToLowerInvariant())) {
        // Parse meta file only locally. Currently we have no use to parse it with custom resolvers.
        if (useDefaultResolver) {
          parseBufferLocally(new SimpleBuffer(metaFileContentsBuffer, metaFileContentsBuffer.Length), guidsInFile);
        }
      }
      else {
        // Parse main file only if it is not an image asset.
        
        var buffer = parseFileOptimized_bufferCache.Value;

        var length = SimpleBuffer.readAllBytesFast(assetPath, ref buffer);
        var simpleBuffer = new SimpleBuffer(buffer, length);
        if (useDefaultResolver) {
          parseBufferLocally(simpleBuffer, guidsInFile);
        }
        parseBufferWithExtraResolvers(simpleBuffer, guidsInFile, extraResolvers);

        parseFileOptimized_bufferCache.Value = buffer;
      }

      return Some.a(new ParseFileResult(assetGuid: guid, assetPath: assetPath, guidsInFile));

      static void parseBufferLocally(SimpleBuffer simpleBuffer, GuidsInFile guidsInFile) {
        for (var i = 0; i < simpleBuffer.length; i++) {
          // Regex for reference: @"{fileID:\s+(\d+),\s+guid:\s+(\w+),\s+type:\s+\d+}",
          if (!simpleBuffer.match(i, STRING_FILE_ID)) continue;
          i += STRING_FILE_ID.Length;
          simpleBuffer.skipWhitespace(ref i);
          
          if (!simpleBuffer.readLong(ref i, out var fileId)) continue;
          
          simpleBuffer.skipOptionalCharacter(ref i, '-');
          simpleBuffer.skipNumerals(ref i);
          
          if (!simpleBuffer.skipCharacter(ref i, ',')) continue;
          simpleBuffer.skipWhitespace(ref i);
          if (!simpleBuffer.match(i, STRING_GUID)) continue;
          i += STRING_GUID.Length;
          simpleBuffer.skipWhitespace(ref i);
          if (simpleBuffer.readUnityGuid(i, out var childGuidStr)) {
            var assetGuid = new AssetGuid(childGuidStr);
            i += assetGuid.guid.Length;
            if (!simpleBuffer.skipCharacter(ref i, ',')) continue;
            
            var fileIds = guidsInFile.guidsInFile.getOrUpdateM(assetGuid, () => new());
            fileIds.Add(new FileId(fileId));
          }
        }
      }

      static void parseBufferWithExtraResolvers(
        SimpleBuffer simpleBuffer, GuidsInFile guidsInFile,
        ImmutableArrayC<BytesParserAndGuidResolver> extraResolvers
      ) {
        foreach (var resolver in extraResolvers) {
          var resolverResults = resolver.parseBuffer(simpleBuffer.buffer, length: simpleBuffer.length);
          foreach (var assetGuid in resolverResults) {
            var fileIds = guidsInFile.guidsInFile.getOrUpdateM(assetGuid, () => new());
            fileIds.Add(FileId.notAvailable);
          }
        }
      }
    }
    
    // /// <summary>
    // /// Unoptimized version of <see cref="parseFileOptimized"/>. Code left here for reference.
    // /// </summary>
    // static void parseFileUnoptimized(
    //   Dictionary<AssetPath, AssetGuid> pathToGuid, ILog log,
    //   AssetPath assetPath,
    //   Dictionary<AssetGuid, HashSet<AssetGuid>> updatedChildren
    // ) {
    //   if (!getGuid(assetPath, out var guid, out _, log)) return;
    //
    //   lock (pathToGuid) pathToGuid[assetPath] = guid;
    //   
    //   var bytes = File.ReadAllBytes(assetPath);
    //   var str = Encoding.ASCII.GetString(bytes);
    //   var m = guidRegex.Match(str);
    //   while (m.Success) {
    //     var childGuid = new AssetGuid(m.Groups[2].Value);
    //     lock (updatedChildren) {
    //       updatedChildren.getOrUpdate(guid, () => new HashSet<AssetGuid>()).Add(childGuid);
    //     }
    //     m = m.NextMatch();
    //   }
    // }

    static void addParents(
      Parents parents,
      Children updatedChildren
    ) {
      foreach (var (parent, children) in updatedChildren.children) {
        foreach (var child in children.guidsInFile.Keys) {
          parents.parents.getOrUpdate(child, () => new HashSet<AssetGuid>()).Add(parent);
        }
      }
    }

    /// <summary>
    /// Uses out to return instead of Either for performance reasons.
    /// <para/>
    /// I tried to optimize this method similarly to <see cref="parseFileOptimized"/>, but the improvement was too small.
    /// </summary>
    static bool getGuid(AssetPath assetPath, out AssetGuid guid, out byte[] metaFileContents, ILog log) {
      if (assetPath.path.StartsWithFast("ProjectSettings/")) {
        guid = default;
        metaFileContents = null;
        return false;
      }

      var metaPath = $"{s(assetPath)}.meta";
      if (File.Exists(metaPath)) {
        metaFileContents = File.ReadAllBytes(metaPath);
        var fileContents = Encoding.ASCII.GetString(metaFileContents);
        var m = metaGuid.Match(fileContents);
        if (m.Success) {
          guid = new AssetGuid(m.Groups[1].Value);
          return true;
        }
        else {
          log.error($"Guid not found for: {assetPath}");
          guid = default;
          return false;
        }
      }
      else {
        var isInPackages = assetPath.path.StartsWithFast("Packages/") && Directory.Exists(assetPath.path);
        
        // Sometimes things in 'Packages/' does not have meta files, not much we can do there except ignore that.
        if (!isInPackages) {
          log.error($"Meta file not found for {assetPath}");
        }
        
        guid = default;
        metaFileContents = null;
        return false;
      }
    }

    [Record] readonly partial struct ParseFileResult {
      public readonly AssetGuid assetGuid;
      public readonly AssetPath assetPath;
      public readonly GuidsInFile childGuids;
    }
    
    /// <summary>File ID that is used internally in serialized Unity files.</summary>
    [Record(ConstructorFlags.Constructor), NewTypeImplicitTo] 
    public readonly partial struct FileId {
      public readonly long id;
      
      /// <summary>
      /// All Texture references use this id.
      /// Sprite references use other ids.
      /// </summary>
      public static readonly FileId texture = new(2800000);
      
      /// <summary>
      /// Custom references do not have file ids.
      /// </summary>
      public static readonly FileId notAvailable = new(0);

    }
    
    /// <summary>
    /// Represents all references from a single file to objects in other files.
    /// <para/>
    /// One file may contain many references to other objects (<see cref="AssetGuid"/>s). And each guid reference may
    /// have a different <see cref="FileId"/>
    /// </summary>
    public class GuidsInFile {
      /// <summary>
      /// Key: guid of the asset that is referenced in the file.
      /// Value: unique file ids that are used to reference the asset.
      /// </summary>
      public readonly Dictionary<AssetGuid, HashSet<FileId>> guidsInFile = new();
    }

    /// <summary>
    /// Represents either children or parents of an asset.
    /// </summary>
    public interface Neighbors {
      public Option<IReadOnlyCollection<AssetGuid>> getNeighbors(AssetGuid guid);
    }
    
    public class Children : Neighbors {
      public readonly Dictionary<AssetGuid, GuidsInFile> children = new();

      public Option<IReadOnlyCollection<AssetGuid>> getNeighbors(AssetGuid guid) =>
        children.get(guid).mapM<GuidsInFile, IReadOnlyCollection<AssetGuid>>(_ => _.guidsInFile.Keys);
    }
    
    public class Parents : Neighbors {
      public readonly Dictionary<AssetGuid, HashSet<AssetGuid>> parents = new();
      
      public Option<IReadOnlyCollection<AssetGuid>> getNeighbors(AssetGuid guid) =>
        parents.get(guid).mapM<HashSet<AssetGuid>, IReadOnlyCollection<AssetGuid>>(_ => _);
    }
  }
}
