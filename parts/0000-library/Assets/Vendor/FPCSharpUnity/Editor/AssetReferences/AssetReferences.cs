using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using Sirenix.Utilities;
using FPCSharpUnity.core.collection;
using UnityEditor;
using ImmutableList = System.Collections.Immutable.ImmutableList;

namespace FPCSharpUnity.unity.Editor.AssetReferences {
  public partial class AssetReferences {
    public readonly Dictionary<string, HashSet<string>> parents = new();
    public readonly Dictionary<string, HashSet<string>> children = new();
    readonly Dictionary<string, string> pathToGuid = new();
    public string scanDuration = "";

    AssetReferences() {}

    public static AssetReferences a(
      AssetUpdate data, int workers, Ref<float> progress, ILog log
    ) {
      var refs = new AssetReferences();
      var sw = Stopwatch.StartNew();
      process(
        data, workers,
        pathToGuid: refs.pathToGuid, parents: refs.parents, children: refs.children,
        progress: progress, log: log
      );
      refs.scanDuration = sw.Elapsed.ToString();
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

    public void update(AssetUpdate data, int workers, Ref<float> progress, ILog log) {
      process(
        data, workers,
        pathToGuid: pathToGuid, parents: parents, children: children,
        progress: progress, log: log
      );
    }

    /// <summary>
    /// Given an object GUID find all scenes where that particular GUID is being used.
    /// </summary>
    /// <returns>guids for scenes where given guid is used</returns>
    public System.Collections.Immutable.ImmutableList<Chain> findParentScenes(string guid) => 
      findParentX(guid, path => path.EndsWithFast(".unity"));
    
    /// <summary>
    /// Given an object GUID find all resources where that particular GUID is being used.
    /// </summary>
    /// <returns>guids for resources where given guid is used</returns>
    public System.Collections.Immutable.ImmutableList<Chain> findParentResources(string guid) => 
      findParentX(guid, path => path.ToLowerInvariant().Contains("/resources/"));

    [Record] public sealed partial class Chain {
      public readonly NonEmpty<System.Collections.Immutable.ImmutableList<string>> guids;

      public string mainGuid => guids.head();
    }
    
    public System.Collections.Immutable.ImmutableList<Chain> findParentX(string guid, Func<string, bool> pathPredicate) {
      // TODO: expensive operation. Need to cache results
      // Dijkstra
      
      // guid -> child guid
      var visited = new Dictionary<string, Option<string>>();
      var q = new Queue<(string current, Option<string> child)>();
      q.Enqueue((guid, Option<string>.None));
      var res = ImmutableList.CreateBuilder<Chain>();
      while (q.Count > 0) {
        var (current, maybeChild) = q.Dequeue();
        if (visited.ContainsKey(current)) continue;
        visited.Add(current, maybeChild);
        var path = AssetDatabase.GUIDToAssetPath(current);
        if (pathPredicate(path)) {
          res.Add(makeChain(current));
        }
        if (parents.TryGetValue(current, out var currentParents)) {
          foreach (var parent in currentParents) {
            if (!visited.ContainsKey(parent)) q.Enqueue((parent, Some.a(current)));
          }
        }
      }
      return res.ToImmutable();

      Chain makeChain(string g) {
        // head points to parent, last points to the object from which we started the search
        var builder = ImmutableList.CreateBuilder<string>();
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
      Dictionary<string, string> pathToGuid,
      Dictionary<string, HashSet<string>> parents,
      Dictionary<string, HashSet<string>> children,
      Ref<float> progress, ILog log
    ) {
      progress.value = 0;
      Func<string, bool> predicate = p =>
        p.EndsWithFast(".asset")
        || p.EndsWithFast(".prefab")
        || p.EndsWithFast(".unity")
        || p.EndsWithFast(".mat")
        || p.EndsWithFast(".spriteatlas")
        || p.EndsWithFast(".spriteatlasv2");
      
      var assets = data.filter(predicate, _ => predicate(_.fromPath));
      var firstScan = children.isEmpty() && parents.isEmpty();
      var updatedChildren =
        firstScan
        ? children
        : new Dictionary<string, HashSet<string>>();

      var progressIdx = 0;
      Action updateProgress = () => progress.value = (float)progressIdx / assets.totalAssets;
      
      Parallel.ForEach(
        assets.newAssets, 
        parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = workers },
        localInit: () => new List<ParseFileResult>(), 
        body: (added, loopState, results) => {
          if (parseFileOptimized(log, added).valueOut(out var parseResult)) {
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
              {if (updatedChildren.TryGetValue(result.assetGuid, out var existing)) {
                existing.AddRange(result.childGuids);
              } else {
                updatedChildren.Add(result.assetGuid, result.childGuids);
              }}
            }
          }
        }
      );

      foreach (var deleted in assets.deletedAssets) {
        updateProgress();
        if (pathToGuid.ContainsKey(deleted)) {
          updatedChildren.getOrUpdate(pathToGuid[deleted], () => new HashSet<string>());
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
        foreach (var parent in updatedChildren.Keys) {
          if (children.ContainsKey(parent)) {
            var set = children[parent];
            foreach (var child in set) parents[child].Remove(parent);
            children.Remove(parent);
          }
        }
      }
      addParents(parents, updatedChildren);
      if (!firstScan) {
        foreach (var kv in updatedChildren) {
          if (kv.Value.Count > 0) children.Add(kv.Key, kv.Value);
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
    public static readonly List<BytesParserAndGuidResolver> extraResolvers = new();
    
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
      public abstract ImmutableArrayC<string> parseBuffer(byte[] buffer, int length);
    }
    
    
    /// <summary>
    /// Optimized version of <see cref="parseFileUnoptimized"/>. This works at least 10x faster.
    /// </summary>
    static Option<ParseFileResult> parseFileOptimized(ILog log, string assetPath) {
      if (!getGuid(assetPath, out var guid, log)) return None._;

      var guidsInFile = new HashSet<string>();

      var buffer = parseFileOptimized_bufferCache.Value;

      {
        var length = readAllBytesFast(assetPath, ref buffer);
        var simpleBuffer = new SimpleBuffer(buffer, length);
        for (var i = 0; i < length; i++) {
          // Regex for reference: @"{fileID:\s+(\d+),\s+guid:\s+(\w+),\s+type:\s+\d+}",
          if (!simpleBuffer.match(i, STRING_FILE_ID)) continue;
          i += STRING_FILE_ID.Length;
          simpleBuffer.skipWhitespace(ref i);
          simpleBuffer.skipNumerals(ref i);
          if (!simpleBuffer.skipCharacter(ref i, ',')) continue;
          simpleBuffer.skipWhitespace(ref i);
          if (!simpleBuffer.match(i, STRING_GUID)) continue;
          i += STRING_GUID.Length;
          simpleBuffer.skipWhitespace(ref i);
          var childGuid = simpleBuffer.readGuid(i);
          i += childGuid.Length;
          if (!simpleBuffer.skipCharacter(ref i, ',')) continue;

          guidsInFile.Add(childGuid);
        }
        
        guidsInFile.AddRange(
          extraResolvers.SelectMany(p => p.parseBuffer(buffer, length: length))
        );
      }

      parseFileOptimized_bufferCache.Value = buffer;

      return Some.a(new ParseFileResult(assetGuid: guid, assetPath: assetPath, guidsInFile));
    }
    
    /// <summary>
    /// Unoptimized version of <see cref="parseFileOptimized"/>. Code left here for reference.
    /// </summary>
    static void parseFileUnoptimized(
      Dictionary<string, string> pathToGuid, ILog log,
      string assetPath,
      Dictionary<string, HashSet<string>> updatedChildren
    ) {
      if (!getGuid(assetPath, out var guid, log)) return;

      lock (pathToGuid) pathToGuid[assetPath] = guid;
      
      var bytes = File.ReadAllBytes(assetPath);
      var str = Encoding.ASCII.GetString(bytes);
      var m = guidRegex.Match(str);
      while (m.Success) {
        var childGuid = m.Groups[2].Value;
        lock (updatedChildren) {
          updatedChildren.getOrUpdate(guid, () => new HashSet<string>()).Add(childGuid);
        }
        m = m.NextMatch();
      }
    }
    
    /// <summary>
    /// Fast way to read all bytes from file to memory.
    ///
    /// Code adapted from <see cref="File.ReadAllBytes"/> implementation.
    /// </summary>
    /// <returns>
    /// Length of the bytes read to the buffer. Buffer may be resized if it does not fit the file.
    /// </returns>
    static int readAllBytesFast(string path, ref byte[] buffer) {
      using var fileStream = new FileStream(
        path, FileMode.Open, FileSystemRights.Read, FileShare.Read, 4096, FileOptions.SequentialScan
      );
      var offset = 0;
      var length = fileStream.Length;
      var count = length <= (long) int.MaxValue ? (int) length : throw new Exception("File too big.");
      if (buffer.Length < length) {
        buffer = new byte[length];
      }
      int num;
      for (; count > 0; count -= num)
      {
        num = fileStream.Read(buffer, offset, count);
        if (num == 0)
          throw new Exception("Could not read file.");
        offset += num;
      }
      return (int) length;
    }

    static void addParents(
      Dictionary<string, HashSet<string>> parents,
      Dictionary<string, HashSet<string>> updatedChildren
    ) {
      foreach (var kv in updatedChildren) {
        var parent = kv.Key;
        foreach (var child in kv.Value) {
          parents.getOrUpdate(child, () => new HashSet<string>()).Add(parent);
        }
      }
    }

    /// <summary>
    /// Uses out to return  instead of Either for performance reasons.
    ///
    /// I tried to optimize this method similarly to <see cref="parseFileOptimized"/>, but the improvement was too small.
    /// </summary>
    static bool getGuid(string assetPath, out string guid, ILog log) {
      if (assetPath.StartsWithFast("ProjectSettings/")) {
        guid = null;
        return false;
      }

      var metaPath = $"{assetPath}.meta";
      if (File.Exists(metaPath)) {
        var fileContents = Encoding.ASCII.GetString(File.ReadAllBytes(metaPath));
        var m = metaGuid.Match(fileContents);
        if (m.Success) {
          guid = m.Groups[1].Value;
          return true;
        }
        else {
          log.error($"Guid not found for: {assetPath}");
          guid = null;
          return false;
        }
      }
      else {
        log.error($"Meta file not found for: {assetPath}");
        guid = null;
        return false;
      }
    }

    [Record] readonly partial struct ParseFileResult {
      public readonly string assetGuid, assetPath;
      public readonly HashSet<string> childGuids;
    }
    
    /// <summary>
    /// Simple structure that encapsulates a buffer array and its length.
    /// It also has methods that we need for parsing guids from asset files.
    /// </summary>
    public readonly struct SimpleBuffer {
      readonly byte[] buffer;
      /// <summary>
      /// Length of the data in buffer. Actual <see cref="buffer"/> length may be larger.
      /// </summary>
      readonly int length;

      public SimpleBuffer(byte[] buffer, int length) {
        this.buffer = buffer;
        this.length = length;
      }

      public bool match(int index, byte[] str) {
        for (var i = 0; i < str.Length; i++) {
          if (index + i >= length) return false;
          if (buffer[index + i] != str[i]) return false;
        }
        return true;
      }

      /// <summary>
      /// Is <see cref="char"/> at <see cref="index"/> is a white space character.
      /// </summary>
      /// <param name="index">Index in <see cref="buffer"/>.</param>
      /// <returns></returns>
      public bool isWhiteSpace(int index) => (char)buffer[index] is '\n' or '\r' or ' ' or '\t';
      
      /// <summary>
      /// Skips all whitespace character from given <see cref="index"/> or until buffer end is reached.
      /// </summary>
      /// <param name="index"></param>
      public void skipWhitespace(ref int index) {
        while (
          index < length && isWhiteSpace(index)
        ) {
          index++;
        }
      }
      
      public bool skipCharacter(ref int index, char character) {
        if (index < length && buffer[index] == character) {
          index++;
          return true;
        }
        return false;
      }
      
      public void skipNumerals(ref int index) {
        while (
          index < length && 
          buffer[index] >= (byte) '0' && buffer[index] <= (byte) '9'
        ) {
          index++;
        }
      }
      
      static readonly ThreadLocal<StringBuilder> stringBuilderCache = new(() => new StringBuilder());

      public string readGuid(int index) {
        var sb = stringBuilderCache.Value;
        sb.Clear();
        while (
          index < length && 
          (
            buffer[index] >= (byte) '0' && buffer[index] <= (byte) '9' 
            || buffer[index] >= (byte) 'a' && buffer[index] <= (byte) 'f'
          )
        ) {
          sb.Append((char) buffer[index]);
          index++;
        }
        return sb.ToString();
      }
    }
  }
}
