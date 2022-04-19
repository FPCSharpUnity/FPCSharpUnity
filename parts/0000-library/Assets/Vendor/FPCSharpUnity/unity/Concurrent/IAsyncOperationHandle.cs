using System;
using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.typeclasses;
using UnityEngine;

namespace FPCSharpUnity.unity.Concurrent {
  /// <summary>
  /// Options for IAsyncOperations status values
  /// </summary>
  public enum AsyncOperationStatus
  {
    /// <summary>
    /// Use to indicate that the operation is still in progress.
    /// </summary>
    None,
    /// <summary>
    /// Use to indicate that the operation succeeded.
    /// </summary>
    Succeeded,
    /// <summary>
    /// Use to indicate that the operation failed.
    /// </summary>
    Failed
  }
  
  /// <summary>
  /// Contains download information for async operations.
  /// </summary>
  [Record]
  public readonly partial struct DownloadStatus {
    /// <summary>
    /// The total number of bytes needed to download by the operation and dependencies.
    /// </summary>
    public readonly ulong downloadedBytes;
    /// <summary>
    /// The number of bytes downloaded by the operation and all of its dependencies.
    /// </summary>
    public readonly ulong totalBytes;
    /// <summary>
    /// Is the operation completed. This is used to determine if the computed Percent should be 0 or 1 when TotalBytes is 0.
    /// </summary>
    public readonly bool isDone;
    /// <summary>
    /// Returns the computed percent complete as a float value between 0 &amp; 1.  If TotalBytes == 0, 1 is returned.
    /// </summary>
    public float percent => (totalBytes > 0) ? ((float) downloadedBytes / totalBytes) : (isDone ? 1.0f : 0f);

    /// <summary>Creates a status where 0 out of 0 bytes has been downloaded and the download is marked as done.</summary>
    public static DownloadStatus done = zero(isDone: true);
    
    /// <summary>Creates a status where 0 out of 0 bytes has been downloaded.</summary>
    public static DownloadStatus zero(bool isDone) => new DownloadStatus(0, 0, isDone);
    
    public static readonly Semigroup<DownloadStatus> semigroup = Semigroup.lambda<DownloadStatus>((a, b) => 
      new DownloadStatus(
        downloadedBytes: a.downloadedBytes + b.downloadedBytes,
        totalBytes: a.totalBytes + b.totalBytes,
        isDone: a.isDone && b.isDone
      )
    );
  }
  
  [PublicAPI] public interface IAsyncOperationHandle<A> {
    AsyncOperationStatus status { get; }
    /// <summary>
    /// Combined progress of downloading from internet and loading from disk
    /// </summary>
    float percentComplete { get; }
    /// <summary>
    /// Status about bytes that are downloaded from the internet
    /// </summary>
    DownloadStatus downloadStatus { get; }
    Future<Try<A>> asFuture { get; }
    void release();
  }

  public static class DownloadStatusExts {
    public static DownloadStatus join(this DownloadStatus a, DownloadStatus b) => DownloadStatus.semigroup.add(a, b);

    public static string debugStr(this DownloadStatus ds) =>
      $"{ds.downloadedBytes}/{ds.totalBytes} bytes ({ds.percent * 100} %), isDone = {ds.isDone}";
  }

  [PublicAPI] public static class IASyncOperationHandle_ {
    public static IAsyncOperationHandle<Unit> delayFrames(uint durationInFrames) => 
      new DelayAsyncOperationHandle<Unit>(durationInFrames, Unit._);
    
    public static IAsyncOperationHandle<A> delayFrames<A>(uint durationInFrames, A a) => 
      new DelayAsyncOperationHandle<A>(durationInFrames, a);

    public static IAsyncOperationHandle<Unit> done => DoneAsyncOperationHandle.instance;

    /// <summary>Launches given async operation, retrying it if it fails.</summary>
    /// <param name="launch"></param>
    /// <param name="tryCount">
    /// If None will retry forever. How many times we should try the operation? If lower than 1 will still try at least
    /// 1 time.
    /// </param>
    /// <param name="retryInterval"></param>
    /// <param name="timeContext"></param>
    public static IAsyncOperationHandle<A> withRetries<A>(
      Func<IAsyncOperationHandle<A>> launch, Option<uint> tryCount, Duration retryInterval, ITimeContext timeContext
    ) => new RetryingAsyncOperationHandle<A>(launch, tryCount, retryInterval, timeContext);
  }
  
  [PublicAPI] public static class IAsyncOperationHandleExts {
    public static IAsyncOperationHandle<B> map<A, B>(
      this IAsyncOperationHandle<A> handle, Func<A, IAsyncOperationHandle<A>, B> mapper
    ) => new MappedAsyncOperationHandle<A, B>(handle, a => mapper(a, handle));
    
    public static IAsyncOperationHandle<B> flatMap<A, B>(
      this IAsyncOperationHandle<A> handle, Func<A, IAsyncOperationHandle<A>, IAsyncOperationHandle<B>> mapper, 
      float aHandleProgressPercentage=0.5f
    ) => new FlatMappedAsyncOperationHandle<A, B>(handle, a => mapper(a, handle), aHandleProgressPercentage);

    public static IAsyncOperationHandle<A> delayedFrames<A>(
      this IAsyncOperationHandle<A> handle, uint durationInFrames, 
      float aHandleProgressPercentage=0.5f
    ) => handle.flatMap(
      (a, h) => new DelayAsyncOperationHandle<A>(durationInFrames, a, h.release),
      aHandleProgressPercentage: aHandleProgressPercentage
    );

    public static IAsyncOperationHandle<ImmutableArrayC<A>> sequenceNonFailing<A>(
      this IReadOnlyCollection<IAsyncOperationHandle<A>> collection
    ) => new SequencedNonFailingAsyncOperationHandle<A>(collection);

    public static IAsyncOperationHandle<ImmutableArrayC<Try<A>>> sequence<A>(
      this IReadOnlyCollection<IAsyncOperationHandle<A>> collection
    ) => new SequencedAsyncOperationHandle<A>(collection);
    
    public static Try<A> toTry<A>(this IAsyncOperationHandle<A> handle) {
      if (handle.asFuture.value.valueOut(out var val)) return val;
      return Try<A>.failed(new Exception("Handle is not completed!"));
    }
    
    public static bool isDone<A>(this IAsyncOperationHandle<A> handle) {
      return handle.status is AsyncOperationStatus.Succeeded or AsyncOperationStatus.Failed;
    }
  }
  
#region implementations
  [Record] public sealed partial class HandleStatusOnRelease {
    public readonly AsyncOperationStatus status;
    public readonly float percentComplete;
    public readonly DownloadStatus downloadStatus;

    public bool isDone => status != AsyncOperationStatus.None;
  }

  public sealed class MappedAsyncOperationHandle<A, B> : IAsyncOperationHandle<B> {
    readonly IAsyncOperationHandle<A> handle;
    readonly Func<A, B> mapper;

    public MappedAsyncOperationHandle(IAsyncOperationHandle<A> handle, Func<A, B> mapper) {
      this.handle = handle;
      this.mapper = mapper;
    }

    public AsyncOperationStatus status => handle.status;
    public float percentComplete => handle.percentComplete;
    public DownloadStatus downloadStatus => handle.downloadStatus;
    [LazyProperty] public Future<Try<B>> asFuture => handle.asFuture.map(try_ => try_.map(mapper));
    public void release() => handle.release();
  }

  public sealed class FlatMappedAsyncOperationHandle<A, B> : IAsyncOperationHandle<B> {
    readonly IAsyncOperationHandle<A> aHandle;
    readonly Future<Try<IAsyncOperationHandle<B>>> bHandleF;
    readonly float aHandleProgressPercentage;
    float bHandleProgressPercentage => 1 - aHandleProgressPercentage;

    public FlatMappedAsyncOperationHandle(
      IAsyncOperationHandle<A> handle, Func<A, IAsyncOperationHandle<B>> mapper, float aHandleProgressPercentage
    ) {
      aHandle = handle;
      if (aHandleProgressPercentage < 0 || aHandleProgressPercentage > 1)
        Log.d.error($"{aHandleProgressPercentage.echo()} not within [0..1], clamping");
      this.aHandleProgressPercentage = Mathf.Clamp01(aHandleProgressPercentage);
      bHandleF = handle.asFuture.map(try_ => try_.map(mapper));
    }

    public AsyncOperationStatus status => 
      bHandleF.value.valueOut(out var b) ? b.fold(h => h.status, e => AsyncOperationStatus.Failed) : aHandle.status;

    public float percentComplete => 
      bHandleF.value.valueOut(out var b) 
        ? aHandleProgressPercentage + b.fold(h => h.percentComplete, e => 1) * bHandleProgressPercentage 
        : aHandle.percentComplete * aHandleProgressPercentage;

    public DownloadStatus downloadStatus {
      get {
        if (bHandleF.value.valueOut(out var b)) {
          return b.fold(h => h.downloadStatus, e => aHandle.downloadStatus);
        }
        return aHandle.downloadStatus;
      }
    }

    public Future<Try<B>> asFuture => bHandleF.flatMapT(bHandle => bHandle.asFuture);

    public void release() {
      { if (bHandleF.value.valueOut(out var b) && b.valueOut(out var h)) h.release(); }
      aHandle.release();
    }
  }

  public sealed class DelayAsyncOperationHandle<A> : IAsyncOperationHandle<A> {
    public readonly uint startedAtFrame, endAtFrame, durationInFrames;
    readonly Option<Action> onRelease;
    readonly A value;

    public DelayAsyncOperationHandle(uint durationInFrames, A value, Action onRelease=null) {
      this.durationInFrames = durationInFrames;
      startedAtFrame = Time.frameCount.toUIntClamped();
      endAtFrame = startedAtFrame + durationInFrames;
      this.value = value;
      this.onRelease = onRelease.opt();
    }

    long framesPassed => Time.frameCount - startedAtFrame;
    long framesLeft => endAtFrame - Time.frameCount;

    public override string ToString() => 
      $"{nameof(DelayAsyncOperationHandle<A>)}({startedAtFrame.echo()}, {endAtFrame.echo()})";

    public AsyncOperationStatus status => isDone ? AsyncOperationStatus.Succeeded : AsyncOperationStatus.None;
    public bool isDone => Time.frameCount >= endAtFrame;
    public float percentComplete => Mathf.Clamp01(framesPassed / (float) durationInFrames);
    public DownloadStatus downloadStatus => DownloadStatus.zero(isDone);

    public Future<Try<A>> asFuture {
      get {
        var left = framesLeft;
        return left <= 0 
          ? Future.successful(Try.value(value)) : FutureU.delayFrames(left.toIntClamped(), Try.value(value));
      }
    }

    public void release() { if (onRelease.valueOut(out var action)) action(); }
  }
  
  public sealed class FutureAsyncOperationHandle<A> : IAsyncOperationHandle<A> {
    readonly Future<A> future;

    public FutureAsyncOperationHandle(Future<A> future) => this.future = future;

    public override string ToString() => 
      $"{nameof(FutureAsyncOperationHandle<A>)}({future.echo()})";

    public AsyncOperationStatus status => isDone ? AsyncOperationStatus.Succeeded : AsyncOperationStatus.None;
    public bool isDone => future.isCompleted;
    public float percentComplete => isDone ? 1 : 0;
    public DownloadStatus downloadStatus => DownloadStatus.zero(isDone);

    public Future<Try<A>> asFuture => future.map(Try.value);

    public void release() {  }
  }

  public static class DoneAsyncOperationHandle {
    public static readonly ConstantAsyncOperationHandle<Unit> instance = new(Unit._);
  }

  /// <summary>
  /// Turns a value that we already have into <see cref="IAsyncOperationHandle{A}"/>.
  /// </summary>
  [Record(ConstructorFlags.Constructor | ConstructorFlags.Apply)]
  public sealed partial class ConstantAsyncOperationHandle<A> : IAsyncOperationHandle<A> {
    public readonly A value;
    
    public AsyncOperationStatus status => AsyncOperationStatus.Succeeded;
    public float percentComplete => 1;
    public DownloadStatus downloadStatus => DownloadStatus.zero(isDone: true);
    public Future<Try<A>> asFuture => Future.successful(Try.value(value));
    public void release() {}
  }

  public sealed class SequencedAsyncOperationHandle<A> : IAsyncOperationHandle<ImmutableArrayC<Try<A>>> {
    public readonly IReadOnlyCollection<IAsyncOperationHandle<A>> handles;

    public SequencedAsyncOperationHandle(IReadOnlyCollection<IAsyncOperationHandle<A>> handles) => 
      this.handles = handles;

    public AsyncOperationStatus status {
      get {
        foreach (var handle in handles) {
          switch (handle.status) {
            case AsyncOperationStatus.None: return AsyncOperationStatus.None;
            case AsyncOperationStatus.Failed: return AsyncOperationStatus.Failed;
            case AsyncOperationStatus.Succeeded: break;
            default: throw new ArgumentOutOfRangeException();
          }
        }

        return AsyncOperationStatus.Succeeded;
      }
    }
    public float percentComplete => handles.Count == 0 ? 1 : handles.Average(_ => _.percentComplete);
    public DownloadStatus downloadStatus => handles.Aggregate(
      DownloadStatus.done, 
      (a, b) => a.join(b.downloadStatus)
    ); 

    public Future<Try<ImmutableArrayC<Try<A>>>> asFuture =>
      handles.Select(h => h.asFuture).parallel().map(arr => Try.value(ImmutableArrayC.move(arr)));
    public void release() { foreach (var handle in handles) handle.release(); }
  }

  public sealed class SequencedNonFailingAsyncOperationHandle<A> : IAsyncOperationHandle<ImmutableArrayC<A>> {
    public readonly IReadOnlyCollection<IAsyncOperationHandle<A>> handles;

    public SequencedNonFailingAsyncOperationHandle(IReadOnlyCollection<IAsyncOperationHandle<A>> handles) => 
      this.handles = handles;

    public AsyncOperationStatus status {
      get {
        foreach (var handle in handles) {
          switch (handle.status) {
            case AsyncOperationStatus.None: return AsyncOperationStatus.None;
            case AsyncOperationStatus.Failed: return AsyncOperationStatus.Failed;
            case AsyncOperationStatus.Succeeded: break;
            default: throw new ArgumentOutOfRangeException();
          }
        }

        return AsyncOperationStatus.Succeeded;
      }
    }
    public float percentComplete => handles.Count == 0 ? 1 : handles.Average(_ => _.percentComplete);
    public DownloadStatus downloadStatus => handles.Aggregate(
      DownloadStatus.done,
      (a, b) => a.join(b.downloadStatus)
    ); 

    public Future<Try<ImmutableArrayC<A>>> asFuture =>
      handles.Select(h => h.asFuture).parallel().map(arr => arr.sequence().map(_ => _.toImmutableArrayC()));
    public void release() { foreach (var handle in handles) handle.release(); }
  }

  public sealed class RetryingAsyncOperationHandle<A> : IAsyncOperationHandle<A> {
    enum State : byte { Launched, WaitingToRetry, Finished, Released }
    
    readonly Func<IAsyncOperationHandle<A>> launchRaw;
    readonly Option<uint> tryCount;
    readonly Duration retryInterval;
    readonly ITimeContext timeContext;
    readonly Future<IAsyncOperationHandle<A>> finalHandleFuture;
    readonly Promise<IAsyncOperationHandle<A>> finalHandlePromise;

    uint retryNo = 1;
    State state;
    IAsyncOperationHandle<A> current;
    IDisposable currentRetryWait = F.emptyDisposable;

    public RetryingAsyncOperationHandle(
      Func<IAsyncOperationHandle<A>> launch, Option<uint> tryCount, Duration retryInterval, ITimeContext timeContext
    ) {
      launchRaw = launch;
      this.tryCount = tryCount;
      this.retryInterval = retryInterval;
      this.timeContext = timeContext;
      finalHandleFuture = Future.async(out finalHandlePromise);

      this.launch();
    }

    public AsyncOperationStatus status => current.status;
    public float percentComplete => current.percentComplete;
    public DownloadStatus downloadStatus => current.downloadStatus;
    public Future<Try<A>> asFuture => finalHandleFuture.flatMap(h => h.asFuture);

    public void release() {
      current.release();
      state = State.Released;
      currentRetryWait.Dispose();
    }

    void launch() {
      var handle = current = launchRaw();
      state = State.Launched;
      handle.asFuture.onComplete(try_ => {
        if (state == State.Released) return;
        
        try_.voidFold(
          // Success!
          a => {
            state = State.Finished;
            finalHandlePromise.complete(handle);
          },
          err => {
            if (!tryCount.valueOut(out var count) || retryNo < count) {
              // Retry
              retryNo++;
              state = State.WaitingToRetry;
              currentRetryWait = timeContext.after(
                retryInterval, name: nameof(RetryingAsyncOperationHandle<A>), act: launch
              );
            }
            else {
              // We've run out of retries, complete with what we had last.
              state = State.Finished;
              finalHandlePromise.complete(handle);
            }
          }
        );
      });
    }
  }
#endregion
}