using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.typeclasses;
using UnityEditor;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.Editor.Utils {
  /// <summary>
  /// Better version of EditorUtility progress bars that does not lag and also logs to log.
  /// </summary>
  [PublicAPI] public sealed class EditorProgress : IDisposable {
    const string NONE = "none";

    public readonly string title;
    public readonly Duration minTimeBeforeShowingUi;

    readonly Stopwatch sw = new Stopwatch();
    readonly ILog log;
    readonly DateTime creationTime = DateTime.Now;

    string current = NONE;
    DateTime
      lastProgressUIUpdate = DateTime.MinValue,
      lastProgressLogUpdate = DateTime.MinValue;

    /// <param name="title"></param>
    /// <param name="log"></param>
    /// <param name="minTimeBeforeShowingUi">
    /// Duration to wait before allowing to show the progressbar dialog.
    /// Use case: When tasks using <see cref="EditorProgress"/> are called frequently (ex. every time you save project)
    /// and it takes a short amount of time (ex. loading small amount of files), that can only be seen as a flicker.
    /// You can prevent this, by setting <see cref="minTimeBeforeShowingUi"/> to a certain value to not allow
    /// progressbar dialog appear in screen before time after creation passes this value. Default is to show instantly.
    /// </param>
    public EditorProgress(string title, ILog log = null, Duration minTimeBeforeShowingUi = default) {
      this.title = title;
      this.minTimeBeforeShowingUi = minTimeBeforeShowingUi;
      this.log = (log ?? Log.@default).withScope($"{s(title)}");
    }

    public void execute(string name, Action a) =>
      execute(name, () => { a(); return F.unit; });

    public A execute<A>(string name, Func<A> f) {
      start(name);
      var ret = f();
      done();
      return ret;
    }

    /// <summary>
    /// Allows using the "using" syntax.
    /// </summary>
    /// <example><![CDATA[
    /// using (editorProgress.executeDisposable("Reimport prefab")) {
    ///   using var _ = AssetDatabaseUtils.doAssetEditing();
    ///   for (var idx = 0; idx <= prefabs.Length; idx++) {
    ///     editorProgress.progress(idx, prefabs.Length);
    ///     var prefab = prefabs[idx];
    ///     AssetDatabase.ImportAsset(prefab, ImportAssetOptions.ForceUpdate);
    ///   }
    /// }
    /// ]]></example>
    public ActionOnDispose executeDisposable(string name) {
      start(name);
      return new ActionOnDispose(done);
    }

    public void start(string name) {
      sw.Reset();
      sw.Start();
      current = name;
      if (log.isInfo()) log.info($"Running {s(name)}...");
      _showProgressBar($"Running {s(name)}...", 0);
      lastProgressUIUpdate = lastProgressLogUpdate = DateTime.MinValue;
    }

    bool _progress(int idx, int total, bool cancellable) {
      var now = DateTime.Now;
      var needsUpdate = idx == 0 || idx == total - 1;
      // Updating progress for every call is expensive, only show every X ms.
      var updateUI = needsUpdate || (now - lastProgressUIUpdate).TotalSeconds >= 0.2;
      var updateLog = log.isInfo() && (needsUpdate || (now - lastProgressLogUpdate).TotalSeconds >= 5);

      var canceled = false;
      if (updateUI || updateLog) {
        var item = idx + 1;
        var msg = $"{s(current)} ({s(item)}/{s(total)})...";

        if (updateUI) {
          canceled = _showProgressBar(msg, (float) item / total, cancellable);
          lastProgressUIUpdate = now;
        }
        if (updateLog) {
          log.info(msg);
          lastProgressLogUpdate = now;
        }
      }
      return canceled;
    }

    bool _showProgressBar(string msg, float progress, bool cancelable = false) {
      if ((DateTime.Now - creationTime).TotalSeconds >= minTimeBeforeShowingUi.seconds) {
        if (cancelable)
          return EditorUtility.DisplayCancelableProgressBar(title, msg, progress);
        EditorUtility.DisplayProgressBar(title, msg, progress);
      }
      return false;
    }

    public void progress(int idx, int total) => _progress(idx, total, false);

    /// <summary>Wrapper for Unity CancelableProgressBar</summary>
    /// <returns>True if task was canceled</returns>
    public bool progressCancellable(int idx, int total) => _progress(idx, total, true);

    /// <summary>A simple method to measure execution times between calls</summary>
    public void done() {
      var duration = new Duration(sw.ElapsedMilliseconds.toIntClamped());
      if (log.isInfo()) log.info($"{s(current)} done in {s(duration.toTimeSpan.toHumanString())}");
      _showProgressBar($"{s(current)} done in {s(duration.toTimeSpan.toHumanString())}.", 1);
      current = NONE;
    }

    /// <summary>
    /// Runs a cancellable multi-threaded processing (using TPL).
    /// </summary>
    /// <param name="name">Name to show for the progress window.</param>
    /// <param name="items">Items to process.</param>
    /// <param name="onItem">Invoked for each item.</param>
    public void progressCancellableParallel<A>(string name, IReadOnlyCollection<A> items, Action<A> onItem) {
      start(name);
      // long because Interlocked only supports that.
      var cancelled = 0L;
      var countStartedProcessing = 0L;
      var countFinishedProcessing = 0L;
      
      // Schedule the items to be processed in parallel. 
      foreach (var item in items) {
        Task.Run(() => {
          // ReSharper disable once AccessToModifiedClosure
          if (Interlocked.Read(ref cancelled) != 0) {
            Interlocked.Increment(ref countFinishedProcessing);
          }
          else {
            // ReSharper disable once AccessToModifiedClosure
            Interlocked.Increment(ref countStartedProcessing);
            try {
              onItem(item);
            }
            finally {
              Interlocked.Increment(ref countFinishedProcessing);
            }
          }
        });
      }

      // Render the progress from the Unity main thread.
      while (shouldKeepGoing()) {
        var idx = Interlocked.Read(ref countStartedProcessing) - 1;
        var cancelledBool = progressCancellable(idx.toIntClamped(), items.Count);
        if (cancelledBool) Interlocked.Increment(ref cancelled);
        
        // Re-render every once in a while.
        Thread.Sleep(200.millis());
      }

      if (Interlocked.Read(ref cancelled) != 0) {
        log.mInfo($"{s(name)} was cancelled: processed items: {s(countFinishedProcessing)} / {s(items.Count)}");
      }
      
      done();

      bool shouldKeepGoing() =>
        // Keep going if not cancelled.
        Interlocked.Read(ref cancelled) == 0
        // And there is still jobs left.
        && Interlocked.Read(ref countFinishedProcessing) != items.Count;
    }

    public void Dispose() { EditorUtility.ClearProgressBar(); }
  }
}
