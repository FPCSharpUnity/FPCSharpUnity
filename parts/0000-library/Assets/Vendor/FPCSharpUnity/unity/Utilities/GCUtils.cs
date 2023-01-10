using System;
using FPCSharpUnity.core.exts;
using GenerationAttributes;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace FPCSharpUnity.unity.Utilities; 

/// <summary>
/// Various utilities related to garbage collection.
/// </summary>
public static partial class GCUtils {
  /// <summary>
  /// Runs either the regular garbage collector or the incremental one (if it is enabled).
  /// </summary>
  /// <param name="maxNanosForIncrementalGC">
  /// Maximum time in nanoseconds allowed to spend for the incremental garbage collector (if it is enabled). Unbounded
  /// if not specified.
  /// </param>
  public static void runGC(ulong? maxNanosForIncrementalGC = null) {
    if (GarbageCollector.isIncremental) GarbageCollector.CollectIncremental(maxNanosForIncrementalGC ?? ulong.MaxValue);
    else GC.Collect();
  }
  
  [Record] public sealed partial class MemoryStats {
    /// <inheritdoc cref="Profiler.GetTotalAllocatedMemoryLong"/>
    public readonly long totalAllocatedMemory;
    
    /// <inheritdoc cref="Profiler.GetTotalReservedMemoryLong"/>
    public readonly long totalReservedMemory;
    
    /// <inheritdoc cref="Profiler.GetTotalUnusedReservedMemoryLong"/>
    public readonly long totalUnusedReservedMemory;
    
    /// <inheritdoc cref="Profiler.GetMonoUsedSizeLong"/>
    public readonly long monoUsedSize;
    
    /// <inheritdoc cref="Profiler.GetMonoHeapSizeLong"/>
    public readonly long monoHeapSize;
    
    /// <inheritdoc cref="Profiler.GetTempAllocatorSize"/>
    public readonly long tempAllocator;
    
    /// <inheritdoc cref="Profiler.GetAllocatedMemoryForGraphicsDriver"/>
    public readonly long graphicsDriver;

    /// <summary>Renders a debug string of memory difference.</summary>
    /// <param name="pre">Memory before an action.</param>
    /// <param name="post">Memory stats after an action.</param>
    public static string differenceString(MemoryStats pre, MemoryStats post) {
      var diff = post - pre;
      return $@"
totalAllocatedMemory
pre = {pre.totalAllocatedMemory.toBytesReadable()}\tpost = {post.totalAllocatedMemory.toBytesReadable()}\tdiff = {diff.totalAllocatedMemory.toBytesReadable()}

totalReservedMemory
pre = {pre.totalReservedMemory.toBytesReadable()}\tpost = {post.totalReservedMemory.toBytesReadable()}\tdiff = {diff.totalReservedMemory.toBytesReadable()}

totalUnusedReservedMemory
pre = {pre.totalUnusedReservedMemory.toBytesReadable()}\tpost = {post.totalUnusedReservedMemory.toBytesReadable()}\tdiff = {diff.totalUnusedReservedMemory.toBytesReadable()}

monoUsedSize
pre = {pre.monoUsedSize.toBytesReadable()}\tpost = {post.monoUsedSize.toBytesReadable()}\tdiff = {diff.monoUsedSize.toBytesReadable()}

monoHeapSize
pre = {pre.monoHeapSize.toBytesReadable()}\tpost = {post.monoHeapSize.toBytesReadable()}\tdiff = {diff.monoHeapSize.toBytesReadable()}

tempAllocator
pre = {pre.tempAllocator.toBytesReadable()}\tpost = {post.tempAllocator.toBytesReadable()}\tdiff = {diff.tempAllocator.toBytesReadable()}

graphicsDriver
pre = {pre.graphicsDriver.toBytesReadable()}\tpost = {post.graphicsDriver.toBytesReadable()}\tdiff = {diff.graphicsDriver.toBytesReadable()}
".Trim();
    }

    public static MemoryStats operator -(MemoryStats a, MemoryStats b) => new MemoryStats(
      totalAllocatedMemory: a.totalAllocatedMemory - b.totalAllocatedMemory,
      totalReservedMemory: a.totalReservedMemory - b.totalReservedMemory,
      totalUnusedReservedMemory: a.totalUnusedReservedMemory - b.totalUnusedReservedMemory,
      monoUsedSize: a.monoUsedSize - b.monoUsedSize,
      monoHeapSize: a.monoHeapSize - b.monoHeapSize,
      tempAllocator: a.tempAllocator - b.tempAllocator,
      graphicsDriver: a.graphicsDriver - b.graphicsDriver
    );
            
    public static MemoryStats get() =>
      new (
        totalAllocatedMemory: Profiler.GetTotalAllocatedMemoryLong(),
        totalReservedMemory: Profiler.GetTotalReservedMemoryLong(),
        totalUnusedReservedMemory: Profiler.GetTotalUnusedReservedMemoryLong(),
        monoUsedSize: Profiler.GetMonoUsedSizeLong(),
        monoHeapSize: Profiler.GetMonoHeapSizeLong(),
        tempAllocator: Profiler.GetTempAllocatorSize().toLong(),
        graphicsDriver: Profiler.GetAllocatedMemoryForGraphicsDriver()
      );
  }
}