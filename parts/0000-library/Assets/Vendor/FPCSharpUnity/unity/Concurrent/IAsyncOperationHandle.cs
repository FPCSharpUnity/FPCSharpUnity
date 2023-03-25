using System;
using System.Collections.Generic;
using System.Linq;
using ExhaustiveMatching;
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
  /// Options for <see cref="IAsyncOperationHandle{A}"/> status values. This basically expresses the type of
  /// <see cref="IAsyncOperationHandle{A}.asFuture"/> as an enum.
  /// </summary>
  public enum IAsyncOperationHandleStatus {
    /// <summary>Operation is still in progress.</summary>
    InProgress,
    
    /// <summary>Operation succeeded.</summary>
    Succeeded,
    
    /// <summary>Operation failed.</summary>
    Failed,
    
    /// <summary>Operation was cancelled before it could finish.</summary>
    Cancelled
  }
  
  /// <summary>
  /// Contains download information for async operations if they download things from the internet.
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
    public float percent => totalBytes > 0 ? (float) downloadedBytes / totalBytes : (isDone ? 1.0f : 0f);
    
    /// <summary>Creates a status where 0 out of 0 bytes has been downloaded and the download is marked as done.</summary>
    public static DownloadStatus done = zero(isDone: true);
    
    /// <summary>Creates a status where 0 out of 0 bytes has been downloaded.</summary>
    public static DownloadStatus zero(bool isDone) => new DownloadStatus(0, 0, isDone);

    public static DownloadStatus operator +(DownloadStatus a, DownloadStatus b) =>
      new DownloadStatus(
        downloadedBytes: a.downloadedBytes + b.downloadedBytes,
        totalBytes: a.totalBytes + b.totalBytes,
        isDone: a.isDone && b.isDone
      );
    
    public static readonly Semigroup<DownloadStatus> semigroup = Semigroup.lambda<DownloadStatus>((a, b) => a + b);
    
    public string debugStr =>
      $"{downloadedBytes}/{totalBytes} bytes ({percent * 100} %), isDone = {isDone}";
  }

  /// <summary>Progress of an <see cref="IAsyncOperationHandle{A}"/>.</summary>
  public interface IAsyncOperationProgress {
    /// <summary>Total progress of the operation in the range of [0..1].</summary>
    float percentComplete { get; }
    
    /// <inheritdoc cref="DownloadStatus"/>
    DownloadStatus downloadStatus { get; }
  }

  /// <summary>
  /// The <see cref="IAsyncOperationHandle{A}"/> has been cancelled before it has finished.
  /// </summary>
  [Record] public sealed partial class IAsyncOperationHandleCancelled : IAsyncOperationProgress {
    public float percentComplete { get; }
    public DownloadStatus downloadStatus { get; }

    /// <summary>Copies the progress from the provided object.</summary>
    public static IAsyncOperationHandleCancelled copyFrom(IAsyncOperationProgress progress) => new(
      percentComplete: progress.percentComplete,
      downloadStatus: progress.downloadStatus
    );
  }
  
  /// <summary>
  /// Represents a handle to ongoing asynchronous operation.
  /// <para/>
  /// You should implement this on your own asynchronous operations to get support for various combinators that are
  /// available for this type.
  /// </summary>
  [PublicAPI] public interface IAsyncOperationHandle<A> : IAsyncOperationProgress {
    /// <summary>
    /// Returns a future that:
    /// <list type="bullet">
    /// <item>
    ///   If the operation was cancelled via <see cref="release"/> before it could finish, it will return
    ///   <see cref="IAsyncOperationHandleCancelled"/>.
    /// </item>
    /// <item>If the operation finished, but failed somehow, it will have an <see cref="Exception"/>.</item>
    /// <item>If the operation succeeded, it will have <see cref="A"/>.</item> 
    /// </list>
    /// </summary>
    Future<Either<IAsyncOperationHandleCancelled, Try<A>>> asFuture { get; }
    
    /// <summary>
    /// Releases the resources after this operation is not needed anymore.
    /// <para/>
    /// If the operation is still in progress this should stop the ongoing progress. See <see cref="asFuture"/> for
    /// documentation how it interacts with <see cref="release"/>.
    /// </summary>
    void release();
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
      Func<IAsyncOperationHandle<A>> launch, Option<uint> tryCount, Duration retryInterval, ITimeContextUnity timeContext
    ) => new RetryingAsyncOperationHandle<A>(launch, tryCount, retryInterval, timeContext);
  }
  
  [PublicAPI] public static class IAsyncOperationHandleExts {
    /// <summary>See <see cref="IAsyncOperationHandleStatus"/>.</summary>
    public static IAsyncOperationHandleStatus status<A>(
      this IAsyncOperationHandle<A> handle
    ) => handle.asFuture.value.foldM(
      IAsyncOperationHandleStatus.InProgress,
      static either => either.foldM(
        IAsyncOperationHandleStatus.Cancelled, 
        static @try => @try.isSuccess ? IAsyncOperationHandleStatus.Succeeded : IAsyncOperationHandleStatus.Failed
      )
    );

    /// <summary>
    /// Special case of <see cref="IAsyncOperationHandle{A}.asFuture"/> where the
    /// <see cref="IAsyncOperationHandleCancelled"/> is expressed as an <see cref="Exception"/>. 
    /// </summary>
    public static Future<Try<A>> asFutureSimple<A>(this IAsyncOperationHandle<A> handle) =>
      handle.asFuture.map(either => either.getOrElseM(cancelled => Try<A>.failed(new Exception(
        $"{nameof(IAsyncOperationHandle<A>)} was cancelled: {cancelled}"
      ))));
    
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
    
    public static Try<A> toTry<A>(this IAsyncOperationHandle<A> handle) => 
      handle.asFuture.value.valueOut(out var either) 
        ? either.getOrElseM(() => Try<A>.failed(new Exception("Operation was cancelled."))) 
        : Try<A>.failed(new Exception("Handle is not completed!"));

    /// <summary>
    /// Returns `true` if the status is done changing (it cannot change anymore). 
    /// </summary>
    public static bool isDone<A>(this IAsyncOperationHandle<A> handle) {
      var status = handle.status();
      return status switch {
        IAsyncOperationHandleStatus.InProgress => false,
        IAsyncOperationHandleStatus.Succeeded => true,
        IAsyncOperationHandleStatus.Failed => true,
        IAsyncOperationHandleStatus.Cancelled => true,
        _ => throw ExhaustiveMatch.Failed(status)
      };
    }
  }
  
#region implementations
  /// <summary>
  /// Captures the state of an <see cref="IAsyncOperationHandle{A}"/> when
  /// <see cref="IAsyncOperationHandle{A}.release"/> is invoked.
  /// </summary>
  [Record] public sealed partial class IAsyncOperationHandleStatusOnRelease : IAsyncOperationProgress {
    public readonly IAsyncOperationHandleStatus status;
    public float percentComplete { get; }
    public DownloadStatus downloadStatus { get; }
  }

  public sealed class MappedAsyncOperationHandle<A, B> : IAsyncOperationHandle<B> {
    readonly IAsyncOperationHandle<A> handle;
    readonly Func<A, B> mapper;

    public MappedAsyncOperationHandle(IAsyncOperationHandle<A> handle, Func<A, B> mapper) {
      this.handle = handle;
      this.mapper = mapper;
    }

    public float percentComplete => handle.percentComplete;
    public DownloadStatus downloadStatus => handle.downloadStatus;
    
    [LazyProperty] public Future<Either<IAsyncOperationHandleCancelled, Try<B>>> asFuture => 
      handle.asFuture.mapT(try_ => try_.map(mapper));
    
    public void release() => handle.release();
  }

  public sealed class FlatMappedAsyncOperationHandle<A, B> : IAsyncOperationHandle<B> {
    readonly IAsyncOperationHandle<A> aHandle;
    readonly Future<Either<IAsyncOperationHandleCancelled, Try<IAsyncOperationHandle<B>>>> bHandleF;
    readonly float aHandleProgressPercentage;
    float bHandleProgressPercentage => 1 - aHandleProgressPercentage;

    public FlatMappedAsyncOperationHandle(
      IAsyncOperationHandle<A> handle, Func<A, IAsyncOperationHandle<B>> mapper, float aHandleProgressPercentage
    ) {
      aHandle = handle;
      if (aHandleProgressPercentage < 0 || aHandleProgressPercentage > 1)
        Log.d.error($"{aHandleProgressPercentage.echo()} not within [0..1], clamping");
      this.aHandleProgressPercentage = Mathf.Clamp01(aHandleProgressPercentage);
      bHandleF = handle.asFuture.mapT(try_ => try_.map(mapper));
    }

    public float percentComplete => 
      bHandleF.value.valueOut(out var bEither) 
        ? aHandleProgressPercentage + bEither.foldM(
          cancelled => cancelled.percentComplete,
          b => b.fold(h => h.percentComplete, _ => 1)
        ) * bHandleProgressPercentage
        : aHandle.percentComplete * aHandleProgressPercentage;

    public DownloadStatus downloadStatus =>
      aHandle.downloadStatus + bHandleF.value.foldM(
        static () => DownloadStatus.zero(false),
        static bEither => bEither.foldM(
          static cancelled => cancelled.downloadStatus,
          static b => b.fold(h => h.downloadStatus, _ => DownloadStatus.zero(false))
        )
      );

    [LazyProperty] public Future<Either<IAsyncOperationHandleCancelled, Try<B>>> asFuture => 
      bHandleF.flatMap(either => either.foldM(
        cancelled => Future.successful(Either<IAsyncOperationHandleCancelled, Try<B>>.Left(cancelled)),
        @try => @try.fold(
          handle => handle.asFuture,
          exception => Future.successful(Either<IAsyncOperationHandleCancelled, Try<B>>.Right(exception))
        )
      ));

    public void release() {
      { if (bHandleF.value.flatMapM(_ => _.rightValue).flatMapM(_ => _.toOption()).valueOut(out var h)) h.release(); }
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

    public IAsyncOperationHandleStatus status => isDone ? IAsyncOperationHandleStatus.Succeeded : IAsyncOperationHandleStatus.InProgress;
    public bool isDone => Time.frameCount >= endAtFrame;
    public float percentComplete => Mathf.Clamp01(framesPassed / (float) durationInFrames);
    public DownloadStatus downloadStatus => DownloadStatus.zero(isDone);

    public Future<Either<IAsyncOperationHandleCancelled, Try<A>>> asFuture {
      get {
        var framesLeft = this.framesLeft;
        var rightValue = Either<IAsyncOperationHandleCancelled, Try<A>>.Right(Try.value(value));
        return framesLeft <= 0 
          ? Future.successful(rightValue) 
          : FutureU.delayFrames(framesLeft.toIntClamped(), rightValue);
      }
    }

    public void release() { if (onRelease.valueOut(out var action)) action(); }
  }
  
  public sealed class FutureAsyncOperationHandle<A> : IAsyncOperationHandle<A> {
    readonly Future<A> future;

    public FutureAsyncOperationHandle(Future<A> future) => this.future = future;

    public override string ToString() => 
      $"{nameof(FutureAsyncOperationHandle<A>)}({future.echo()})";

    public IAsyncOperationHandleStatus status => isDone ? IAsyncOperationHandleStatus.Succeeded : IAsyncOperationHandleStatus.InProgress;
    public bool isDone => future.isCompleted;
    public float percentComplete => isDone ? 1 : 0;
    public DownloadStatus downloadStatus => DownloadStatus.zero(isDone);

    [LazyProperty] public Future<Either<IAsyncOperationHandleCancelled, Try<A>>> asFuture => 
      future.map(a => Either<IAsyncOperationHandleCancelled, Try<A>>.Right(Try.value(a)));

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
    
    public IAsyncOperationHandleStatus status => IAsyncOperationHandleStatus.Succeeded;
    public float percentComplete => 1;
    public DownloadStatus downloadStatus => DownloadStatus.zero(isDone: true);
    public Future<Either<IAsyncOperationHandleCancelled, Try<A>>> asFuture => 
      Future.successful(Either<IAsyncOperationHandleCancelled, Try<A>>.Right(Try.value(value)));
    public void release() {}
  }

  public sealed class SequencedAsyncOperationHandle<A> : IAsyncOperationHandle<ImmutableArrayC<Try<A>>> {
    public readonly IReadOnlyCollection<IAsyncOperationHandle<A>> handles;

    public SequencedAsyncOperationHandle(IReadOnlyCollection<IAsyncOperationHandle<A>> handles) => 
      this.handles = handles;

    public float percentComplete => handles.Count == 0 ? 1 : handles.Average(_ => _.percentComplete);
    public DownloadStatus downloadStatus => handles.Aggregate(
      DownloadStatus.done, 
      (a, b) => a + b.downloadStatus
    ); 

    [LazyProperty] public Future<Either<IAsyncOperationHandleCancelled, Try<ImmutableArrayC<Try<A>>>>> asFuture =>
      handles.Select(h => h.asFuture).parallel().map(eithers =>
        eithers.sequence().mapRightM(arr => Try.value(ImmutableArrayC.move(arr)))
      );
    
    public void release() { foreach (var handle in handles) handle.release(); }
  }

  public sealed class SequencedNonFailingAsyncOperationHandle<A> : IAsyncOperationHandle<ImmutableArrayC<A>> {
    public readonly IReadOnlyCollection<IAsyncOperationHandle<A>> handles;

    public SequencedNonFailingAsyncOperationHandle(IReadOnlyCollection<IAsyncOperationHandle<A>> handles) => 
      this.handles = handles;

    public float percentComplete => handles.Count == 0 ? 1 : handles.Average(_ => _.percentComplete);
    public DownloadStatus downloadStatus => handles.Aggregate(
      DownloadStatus.done,
      (a, b) => a + b.downloadStatus
    ); 

    [LazyProperty] public Future<Either<IAsyncOperationHandleCancelled, Try<ImmutableArrayC<A>>>> asFuture =>
      handles.Select(h => h.asFuture).parallel().map(eithers =>
        eithers.sequence().mapRightM(arr => arr.sequence().map(_ => _.toImmutableArrayC()))
      );
    
    public void release() { foreach (var handle in handles) handle.release(); }
  }

  public sealed class RetryingAsyncOperationHandle<A> : IAsyncOperationHandle<A> {
    enum State : byte { Launched, WaitingToRetry, Finished, Released }
    
    readonly Func<IAsyncOperationHandle<A>> launchRaw;
    readonly Option<uint> tryCount;
    readonly Duration retryInterval;
    readonly ITimeContextUnity timeContext;
    readonly Future<Either<IAsyncOperationHandleCancelled, IAsyncOperationHandle<A>>> finalHandleFuture;
    readonly Promise<Either<IAsyncOperationHandleCancelled, IAsyncOperationHandle<A>>> finalHandlePromise;

    uint retryNo = 1;
    State state;
    IAsyncOperationHandle<A> current;
    IDisposable currentRetryWait = F.emptyDisposable;

    public RetryingAsyncOperationHandle(
      Func<IAsyncOperationHandle<A>> launch, Option<uint> tryCount, Duration retryInterval, ITimeContextUnity timeContext
    ) {
      launchRaw = launch;
      this.tryCount = tryCount;
      this.retryInterval = retryInterval;
      this.timeContext = timeContext;
      finalHandleFuture = Future.async(out finalHandlePromise);

      this.launch();
    }

    public float percentComplete => current.percentComplete;
    public DownloadStatus downloadStatus => current.downloadStatus;
    
    [LazyProperty] public Future<Either<IAsyncOperationHandleCancelled, Try<A>>> asFuture => 
      finalHandleFuture.flatMapT(h => h.asFuture);

    public void release() {
      finalHandlePromise.tryComplete(IAsyncOperationHandleCancelled.copyFrom(current));
      current.release();
      state = State.Released;
      currentRetryWait.Dispose();
    }

    void launch() {
      var handle = current = launchRaw();
      state = State.Launched;
      handle.asFuture.onComplete(try_ => {
        if (state == State.Released) return;
        
        try_.voidFoldM(
          // Success!
          a => {
            state = State.Finished;
            finalHandlePromise.complete(Either<IAsyncOperationHandleCancelled, IAsyncOperationHandle<A>>.Right(handle));
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
              finalHandlePromise.complete(Either<IAsyncOperationHandleCancelled, IAsyncOperationHandle<A>>.Right(handle));
            }
          }
        );
      });
    }
  }
#endregion
}