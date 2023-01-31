using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using ExhaustiveMatching;
using FPCSharpUnity.unity.Collection;
using FPCSharpUnity.unity.Components.dispose;
using FPCSharpUnity.unity.Components.ui;
using FPCSharpUnity.unity.Concurrent;
using FPCSharpUnity.core.concurrent;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Dispose;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using FPCSharpUnity.unity.Pools;
using FPCSharpUnity.unity.Reactive;using FPCSharpUnity.core.reactive;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.dispose;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.Utilities;
using UnityEngine;
using UnityEngine.Scripting;
using static FPCSharpUnity.core.typeclasses.Str;
using static FPCSharpUnity.unity.Data.KeyModifier;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Components.DebugConsole {
  [PublicAPI] public partial class DConsole {
    public const string DEFINE_ENABLE_DCONSOLE = "ENABLE_DCONSOLE";
    
    /// <summary>
    /// Maximum amount of entries to keep in <see cref="backgroundLogEntries"/> in non-debug builds.
    /// </summary>
    const int MAX_BACKGROUND_LOG_ENTRY_COUNT_IN_NON_DEBUG_BUILDS = 200;
    
    /// <summary>
    /// Maximum amount of entries to keep in <see cref="backgroundLogEntries"/> in debug builds.
    /// </summary>
    const int MAX_BACKGROUND_LOG_ENTRY_COUNT_IN_DEBUG_BUILDS = 500;
    
    /// <summary>
    /// Maximum amount of lines to add to the view per one frame. We limit this to avoid freezing the player when
    /// there are a lot of log messages to add at once. 
    /// </summary>
    const int BATCH_SIZE_FOR_ADDING_LINES_TO_VIEW =
      // 25 lines is about 1 screen worth of content.
      25;

    /// <summary>
    /// While debug console is not opened we catch the log messages in the background and put them here so that when
    /// we open the debug console we would know at least the last
    /// <see cref="MAX_BACKGROUND_LOG_ENTRY_COUNT_IN_NON_DEBUG_BUILDS"/> log messages.
    /// </summary>
    static readonly Deque<LogEntry> backgroundLogEntries = new();

    static LazyVal<DConsole> _instance = Lazy.a(() => new DConsole().tap(initDConsole));
    public static DConsole instance => _instance.strict;
    
    /// <summary>
    /// If an unlock code is provided to <see cref="show"/> then this holds the state of whether the correct code has
    /// been entered by user.
    /// </summary>
    static bool dConsoleUnlocked;
    
    /// <summary>Will be true if the view is currently instantiated and not minimized.</summary>
    [LazyProperty] public IRxVal<bool> isActiveAndMaximizedRx =>
      currentViewRx.flatMap(static maybeView => 
        maybeView.map(static _ => _.view.maximizedRx).getOrElse(RxVal.staticallyCached(false))
      );
    
    [LazyProperty, Implicit] static ILog log => Log.d.withScope(nameof(DConsole));

    /// <summary>
    /// Registers a handler to Unity that captures the log messages and stores them in
    /// <see cref="backgroundLogEntries"/>. 
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    static void registerLogMessages() {
      if (!Application.isEditor) {
        // In editor we have the editor console, so this is not really needed.
        Application.logMessageReceivedThreaded += (message, stacktrace, type) => {
          var entry = new LogEntry(DateTime.Now, message, type);
          var limit =
            Log.d.isDebug() || Debug.isDebugBuild
              ? MAX_BACKGROUND_LOG_ENTRY_COUNT_IN_DEBUG_BUILDS
              : MAX_BACKGROUND_LOG_ENTRY_COUNT_IN_NON_DEBUG_BUILDS;
          
          lock (backgroundLogEntries) {
            while (backgroundLogEntries.Count > limit) backgroundLogEntries.RemoveFront();
            backgroundLogEntries.Add(entry);
          }
        };
      }
    }

    static void initDConsole(DConsole dConsole) {
      dConsole.registrarOnShow(
        NeverDisposeDisposableTracker.instance, nameof(DConsole),
        (_, r) => {
          r.register("GC mode", () => 
            $"mode={GarbageCollector.GCMode}, incremental={GarbageCollector.isIncremental}, "
            + $"incremental slice={GarbageCollector.incrementalTimeSliceNanoseconds}ns"
          );
          r.register(
            "Memory stats", () => GCUtils.MemoryStats.get().asString(),
            Ctrl+Alt+KeyCode.M
          );
          r.register("Run GC", () => {
            var pre = GCUtils.MemoryStats.get();
            GCUtils.runGC();
            var post = GCUtils.MemoryStats.get();
            return pre.differenceString(post);
          }, Ctrl+Alt+KeyCode.G);
          r.register("Unload Unused Assets", Resources.UnloadUnusedAssets, Ctrl+Alt+KeyCode.U);
          r.register("Self-test", () => "self-test");
          r.register("Future Self-test", () => 
            Future.delay(Duration.fromSeconds(1), () => "after 1 s", TimeContextU.unscaledTime)
          );
          r.register("Re-render", api => api.rerender());
          
          r.register("Clear visible log", clearVisibleLog);
          r.register("Clear saved log", () => {
            lock (backgroundLogEntries) {
              backgroundLogEntries.Clear();
            }

            clearVisibleLog();
          });
        }
      );

      dConsole.registrarOnShow(
        NeverDisposeDisposableTracker.instance, "Scenes in Build List",
        (_, r) => {
          var manager = SceneManagerBetter.instance;
          foreach (var tpl in manager.scenesInBuildSettings.all) {
            r.register($"Load [{s(tpl.buildIndex)}: {s(tpl.path.toSceneName)}]", () => manager.loadScene(tpl.buildIndex));
          }
        }
      );
      
      void clearVisibleLog() {
        foreach (var i in instance.currentViewRx.value) {
          i.dynamicVerticalLayout.clearLayoutData();
        }
      }
    }

    /// <summary>
    /// Invoked when <see cref="DConsole"/> is shown.
    /// </summary>
    public delegate void OnShow(Commands console);
    
    /// <summary>
    /// Set of callbacks to invoke when a <see cref="DConsole"/> is shown.
    /// </summary>
    readonly HashSet<OnShow> onShow = new();

    /// <summary>
    /// As <see cref="registerOnShow"/> but gives you <see cref="DConsoleRegistrar"/> for the given
    /// <see cref="prefix"/>.
    /// </summary>
    /// <param name="tracker">When this is disposed the registration will be destroyed.</param>
    /// <param name="prefix"><see cref="DConsoleRegistrar.commandGroup"/></param>
    /// <param name="action"><see cref="OnShow"/> with the created <see cref="DConsoleRegistrar"/>.</param>
    [Conditional(DEFINE_ENABLE_DCONSOLE)]
    public void registrarOnShow(
      ITracker tracker, string prefix, Action<Commands, DConsoleRegistrar> action,
      [Implicit] CallerData callerData = default  
    ) => 
      registerOnShow(
        tracker, 
        commands => {
          var r = commands.registrarFor(prefix);
          action(commands, r);
        },
        callerData
      );

    /// <summary>
    /// Invokes the given <see cref="OnShow"/> callback when the <see cref="DConsole"/> is shown.
    /// </summary>
    /// <param name="tracker">When this is disposed the registration will be destroyed.</param>
    /// <param name="runOnShow"></param>
    /// <returns>Dispose of me to unregister.</returns>
    [Conditional(DEFINE_ENABLE_DCONSOLE)]
    public void registerOnShow(
      ITracker tracker, OnShow runOnShow, [Implicit] CallerData callerData = default
    ) {
      if (!Application.isPlaying) {
        return;
      }

      ISubscription sub = null;
      sub = new Subscription(() => {
        onShow.Remove(runOnShow);
        
        // ReSharper disable once AccessToModifiedClosure
        tracker.untrack(sub);
      });
      onShow.Add(runOnShow);
      tracker.track(sub, callerData);
    }

    /// <summary>Will be `Some` if a view is currently instantiated.</summary>
    readonly IRxRef<Option<ViewInstance>> currentViewRx = RxRef.a<Option<ViewInstance>>(None._);

#if ENABLE_DCONSOLE
    public static IRxObservable<DebugSequenceInvocationMethod> createDebugSequenceObservable(
      ITracker tracker,
      ITimeContextUnity timeContext = null,
      Option<DebugSequenceMouseData> mouseDataOpt = default,
      Option<DebugSequenceDirectionData> directionDataOpt = default, 
      Option<KeyCodeWithModifiers> keyboardShortcutOpt = default,
      [Implicit] CallerData callerData = default
    ) {
      timeContext ??= TimeContextU.DEFAULT;

      var mouseObs = mouseDataOpt.fold(
        Observable<DebugSequenceInvocationMethod>.empty, 
        mouseData => 
          new RegionClickObservable(mouseData.width, mouseData.height)
            .sequenceWithinTimeframe(tracker, mouseData.sequence, 3, callerData)
            .map(_ => DebugSequenceInvocationMethod.Mouse)
      );

      var directionObs = directionDataOpt.fold(
        Observable<DebugSequenceInvocationMethod>.empty,
        directionData => {
          var directions = ObservableU.everyFrame.collect(_ => {
            var horizontal = Input.GetAxisRaw(directionData.horizontalAxisName);
            var vertical = Input.GetAxisRaw(directionData.verticalAxisName);
            // Both are equal, can't decide.
            if (Math.Abs(horizontal - vertical) < 0.001f) return None._;
            return
              Math.Abs(horizontal) > Math.Abs(vertical)
                ? Some.a(horizontal > 0 ? Direction.Right : Direction.Left)
                : Some.a(vertical > 0 ? Direction.Up : Direction.Down);
          }).changedValues();

          return
            directions
              .withinTimeframe(directionData.sequence.Count, directionData.timeframe, timeContext)
              .filter(l => l.Select(t => t.Item1).SequenceEqual(directionData.sequence))
              .map(_ => DebugSequenceInvocationMethod.UnityInputAxisDirections);
        }
      );

      var keyboardShortcutObs = keyboardShortcutOpt.fold(
        Observable<DebugSequenceInvocationMethod>.empty,
        kc => ObservableU.everyFrame.filter(_ => kc.getKeyDown).map(_ => DebugSequenceInvocationMethod.Keyboard)
      );

      var obs = new [] {mouseObs, directionObs, keyboardShortcutObs}.joinAll();
      return obs;
    }
#endif
    
    /// <summary>
    /// Show <see cref="DConsole"/> <see cref="instance"/> when <see cref="showObservable"/> emits an event.
    /// </summary>
    /// <param name="tracker"></param>
    /// <param name="showObservable">Obtain it via <see cref="createDebugSequenceObservable"/>.</param>
    /// <param name="unlockCode"></param>
    /// <param name="binding"></param>
    [Conditional(DEFINE_ENABLE_DCONSOLE)]
    public static void registerDebugSequence(
      ITracker tracker, 
      IRxObservable<DebugSequenceInvocationMethod> showObservable, Option<string> unlockCode, 
      DebugConsoleBinding binding = null
    ) {
      showObservable.subscribe(tracker, _ => instance.show(unlockCode, binding));
    }
    
    /// <summary>
    /// Shows the debug console. If the console is minimized then just maximizes it.
    /// </summary>
    /// <param name="unlockCode">
    /// If provided the command buttons are not visible until user enters the given unlock code.
    /// </param>
    /// <param name="prefab">Which prefab to use. Uses the default one from resources if not provided.</param>
    [Conditional(DEFINE_ENABLE_DCONSOLE)]
    public void show(Option<string> unlockCode, DebugConsoleBinding prefab = null) {
      // Just maximize it if we already have an instance. 
      {if (currentViewRx.value.valueOut(out var currentInstance)) {
        currentInstance.view.toggleMaximized();
        return;
      }}
      
      prefab = prefab ? prefab : Resources.Load<DebugConsoleBinding>("Debug Console Prefab");

      var commands = new Commands();
      
      invokeOnShowCallbacks(commands);

      BoundButtonList commandButtonList = null;
      var selectedGroup = Option<SelectedGroup>.None;
      var view = prefab.clone();
      view.hideModals();

      // Will get disposed of when the debug console is destroyed.
      var tracker = view.gameObject.asDisposableTracker();
      
      var commandsList = setupList(
        None._, view.commands, clearFilterText: true,
        () => selectedGroup.fold(ImmutableList<ButtonBinding>.Empty, _ => _.commandButtons)
      );
      
      DConsoleCommandAPIImpl apiForClosures = null;
      var api = apiForClosures = new DConsoleCommandAPIImpl(view, rerender: rerender);
      Object.DontDestroyOnLoad(view);

      commandButtonList = setupGroups(clearCommandsFilterText: true);
      
      var logEntryPool = GameObjectPool.a(GameObjectPool.Init<VerticalLayoutLogEntry>.noReparenting(
        nameof(DConsole) + " log entry pool",
        () => view.logEntry.prefab.clone()
      ));

      var layout = new DynamicLayout.Init<DynamicVerticalLayoutLogElementData>(
        view.dynamicLayout, tracker, renderLatestItemsFirst: true
      );
      startAddMessagesToViewCoroutine(layout, view, logEntryPool, tracker);

      // Make sure to clean up on app quit to prevent problems with unity quick play mode enter.
      ASync.onAppQuit.subscribe(view.gameObject.asDisposableTracker(), _ => destroy());
      view.closeButton.onClick.AddListener(destroy);
      view.minimiseButton.onClick.AddListener(view.toggleMaximized);
      view.onUpdate += () => {
        foreach (var (_, list) in commands.dictionary) {
          foreach (var command in list) {
            foreach (var shortcut in command.shortcut) {
              if (shortcut.getKeyDown) {
                command.run(api);
              }
            }
          }
        }
      };

      currentViewRx.value = new ViewInstance(view, layout, logEntryPool, tracker).some();

      BoundButtonList setupGroups(bool clearCommandsFilterText) {
        var groupButtons = commands.dictionary.OrderBySafe(_ => _.Key).Select(commandGroup => {
          var validGroupCommands = commandGroup.Value.Where(cmd => cmd.canShow()).ToArray();
          var button = addButton(view.buttonPrefab, view.commandGroups.holder.transform);
          button.text.text = s(commandGroup.Key);
          button.button.onClick.AddListener(showThisGroup);
          return button;

          void showThisGroup() {
            // ReSharper disable once AccessToModifiedClosure
            var commandButtons = showGroup(view, apiForClosures, commandGroup.Key, validGroupCommands);
            selectedGroup = Some.a(new SelectedGroup(button, commandButtons));
          }
        }).ToImmutableList();
        var list = setupList(
          unlockCode, view.commandGroups, clearFilterText: clearCommandsFilterText, 
          () => groupButtons
        );
        return new BoundButtonList(groupButtons, list);
      }

      void rerender() {
        var maybeSelectedGroupName = selectedGroup.map(_ => _.groupButton.text.text);
        log.mInfo($"Re-rendering DConsole, currently selected group = {maybeSelectedGroupName}.");

        cleanupExistingGroups();
        
        commands = new Commands();
        invokeOnShowCallbacks(commands);
        
        var groups = commandButtonList = setupGroups(clearCommandsFilterText: false);
        reselectPreviousGroup(groups, maybeSelectedGroupName);
      }

      // Clears all of the Unity objects for the buttons for the existing groups.
      void cleanupExistingGroups() {
        // ReSharper disable once AccessToModifiedClosure
        var existingGroups = commandButtonList;
        System.Diagnostics.Debug.Assert(existingGroups != null, nameof(existingGroups) + " != null");
        existingGroups.list.subscription.Dispose();
        foreach (var existingGroup in existingGroups.buttons) {
          existingGroup.button.destroyGameObject();
        }
      }

      void invokeOnShowCallbacks(Commands commands) {
        foreach (var onShow in this.onShow) {
          onShow(commands);
        }
      }

      void reselectPreviousGroup(BoundButtonList groups, Option<string> maybeSelectedGroupName) {
        if (
          maybeSelectedGroupName.valueOut(out var selectedGroupName)
          && groups.buttons.findOut(selectedGroupName, (g, n) => g.text.text == n, out var group)
        ) {
          group.button.onClick.Invoke();
          commandsList.applyFilter();
        }
      }
    }
    
    static SetUpList setupList(
      Option<string> unlockCodeOpt, DebugConsoleListBinding listBinding, bool clearFilterText,
      Func<IEnumerable<ButtonBinding>> contents
    ) {
      listBinding.clearFilterButton.onClick.AddListener(onClearFilter);
      listBinding.filterInput.onValueChanged.AddListener(update);
      if (clearFilterText) listBinding.filterInput.text = "";
      applyFilter();

      var sub = new Subscription(() => {
        listBinding.clearFilterButton.onClick.RemoveListener(onClearFilter);
        listBinding.filterInput.onValueChanged.RemoveListener(update);
      });
      return new SetUpList(applyFilter, sub);
      
      void onClearFilter() => listBinding.filterInput.text = "";
      void applyFilter() => update(listBinding.filterInput.text);

      void update(string query) {
        {if (unlockCodeOpt.valueOut(out var unlockCode)) {
          if (unlockCode.Equals(query, StringComparison.OrdinalIgnoreCase)) {
            dConsoleUnlocked = true;
            // disable filter while query matches unlock code
            query = "";
          }
        }}
        
        var hideButtons = unlockCodeOpt.isSome && !dConsoleUnlocked;
        var showButtons = !hideButtons;
        foreach (var button in contents()) {
          var active = showButtons && button.text.text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
          button.gameObject.SetActive(active);
        }
      }
    }

    static ImmutableList<ButtonBinding> showGroup(
      DebugConsoleBinding view, DConsoleCommandAPI api, GroupName groupName, IEnumerable<Command> commands
    ) {
      view.commandGroupLabel.text = s(groupName);
      var commandsHolder = view.commands.holder;
      foreach (var t in commandsHolder.transform.children()) Object.Destroy(t.gameObject);
      return commands.Select(command => {
        var button = addButton(view.buttonPrefab, commandsHolder.transform);
        button.text.text = command.label;
        button.button.onClick.AddListener(() => command.run(api));
        return button;
      }).ToImmutableList();
    }

    static ButtonBinding addButton(ButtonBinding prefab, Transform target) {
      var button = prefab.clone();
      // Parent of RectTransform is being set with parent property.
      // Consider using the SetParent method instead, with the worldPositionStays
      // argument set to false. This will retain local orientation and scale rather
      // than world orientation and scale, which can prevent common UI scaling issues.
      button.GetComponent<RectTransform>().SetParent(target, worldPositionStays: false);
      return button;
    }

    /// <summary>
    /// Converts the <see cref="LogEntry"/> to one or more <see cref="DynamicVerticalLayoutLogElementData"/>.  
    /// </summary>
    static IEnumerable<DynamicVerticalLayoutLogElementData> createEntries(
      LogEntry data, GameObjectPool<VerticalLayoutLogEntry> pool,
      List<string> cache, float lineWidth
    ) {
      string typeToString(LogType t) =>
        t switch {
          LogType.Error => " ERROR",
          LogType.Assert => " ASSERT",
          LogType.Warning => " WARN",
          LogType.Log => "",
          LogType.Exception => " EXCEPTION",
          _ => throw ExhaustiveMatch.Failed(t)
        };

      Color typeToColor(LogType t) {
        switch (t) {
          case LogType.Error:
          case LogType.Exception:
            return Color.red;
          case LogType.Assert: return Color.magenta;
          case LogType.Warning: return new Color32(213, 144, 0, 255);
          case LogType.Log: return Color.black;
          default: throw ExhaustiveMatch.Failed(t);
        }
      }

      var shortText = $"{data.createdAt:hh:mm:ss}{typeToString(data.type)} {data.message}";

      // letter width can't be smaller, tested on galaxy S5
      const float LETTER_WIDTH = 11.3f;
      var charCount = Mathf.RoundToInt(lineWidth / LETTER_WIDTH);

      var color = typeToColor(data.type);
      shortText.distributeText(charCount, cache);
      for (var idx = cache.Count - 1; idx >= 0; idx--) {
        var e = cache[idx];
        yield return new DynamicVerticalLayoutLogElementData(pool, new VerticalLayoutLogEntry.Data(e, color));
      }
    }
    
    /// <summary>
    /// Creates a double-ended queue for log entries and starts two processes.
    /// <para/>
    /// First one catches the new log messages and puts them to the end of the queue.
    /// <para/>
    /// Second one takes the entries from the queue and puts them into the <see cref="layout"/>.
    /// <para/>
    /// The goal of this is to make sure that we do not block the UI if there are a lot of log entries.
    /// </summary>
    void startAddMessagesToViewCoroutine(
      DynamicLayout.Init<DynamicVerticalLayoutLogElementData> layout,
      DebugConsoleBinding binding,
      GameObjectPool<VerticalLayoutLogEntry> pool,
      ITracker tracker
    ) {
      var messagesToAdd = new Deque<LogEntry>();

      // Copy the existing entries in a blocking fashion and hope that it is fast enough.
      lock (backgroundLogEntries) {
        messagesToAdd.AddRange(backgroundLogEntries);
      }

      var addMessagesToLayoutEnabled = true;

      catchNewIncomingMessages();
      addMessagesToLayoutEveryFrame();
      setupAddMessagesToLayoutEnabledToggling();
      setupClearBackgroundMessages();
      
      void catchNewIncomingMessages() {
        var logCallback = new Application.LogCallback((message, stackTrace, type) => {
          var createdAt = DateTime.Now;
          lock (messagesToAdd) {
            messagesToAdd.Add(new LogEntry(createdAt, message, type));
          }
        });
        tracker.track(() => Application.logMessageReceivedThreaded -= logCallback);
        Application.logMessageReceivedThreaded += logCallback;
      }

      void addMessagesToLayoutEveryFrame() {
        var cache = new List<string>();
        var entriesToAdd = new List<DynamicVerticalLayoutLogElementData>();
        tracker.track(ASync.EveryFrame(() => {
          if (!addMessagesToLayoutEnabled) return true;
          
          entriesToAdd.Clear();
          lock (messagesToAdd) {
            if (!messagesToAdd.IsEmpty) {
              var lineWidth = binding.lineWidth;
              var processedLines = 0;
              while (!messagesToAdd.IsEmpty && processedLines < BATCH_SIZE_FOR_ADDING_LINES_TO_VIEW) {
                var entry = messagesToAdd.RemoveFront();
                foreach (var e in createEntries(entry, pool, cache, lineWidth)) {
                  entriesToAdd.Add(e);
                  processedLines++;
                }
              }
            }
          }
          
          layout.appendDataIntoLayoutData(entriesToAdd);
          entriesToAdd.Clear();
          
          return true;
        }));

        var messagesLeftRef = Ref.withCallback(0, messagesLeft => {
          binding.remainingEntriesLabel.text =
            messagesLeft == 0
              ? "All log entries processed."
              : $"{s(messagesLeft)} log entries remaining.";
        });        
        tracker.track(ASync.EveryXSeconds(0.25f, () => {
          int messagesLeft;
          // Only get the count in the lock to minimize the time it takes.
          lock (messagesToAdd) { messagesLeft = messagesToAdd.Count; }
          // Only change the text if the number has actually changed.
          messagesLeftRef.value = messagesLeft;

          return true;
        }));
      }

      void setupAddMessagesToLayoutEnabledToggling() {
        var button = binding.toggleAddingLogEntriesToViewButton;
        
        setText();
        
        button.button.onClick.AddListener(onClick);
        tracker.track(() => button.button.onClick.RemoveListener(onClick));

        void onClick() {
          addMessagesToLayoutEnabled = !addMessagesToLayoutEnabled;
          setText();
        }

        void setText() {
          button.text.text = addMessagesToLayoutEnabled ? "||" : "|>";
        }
      }

      void setupClearBackgroundMessages() {
        var button = binding.clearBackgroundLogEntriesButton;
        
        button.onClick.AddListener(onClick);
        tracker.track(() => button.onClick.RemoveListener(onClick));

        void onClick() {
          lock (messagesToAdd) {
            messagesToAdd.Clear();
          }
        }
      }
    }

    public void destroy() {
      foreach (var instance in currentViewRx.value) instance.destroy();
      currentViewRx.value = None._;
    }
  }

  public delegate Option<Obj> HasObjFunc<Obj>();
    
  [Record(ConstructorFlags.Apply)] public sealed partial class ButtonData<A> {
    public readonly string label;
    public readonly Action<A> onClick;
  }

  [PublicAPI] public static partial class ButtonData {
    public static readonly ButtonData<DConsoleModalInputAPI> cancel = a<DConsoleModalInputAPI>("Cancel", api => api.closeDialog());
  }

  /// <summary>Set-up button list instance.</summary>
  [Record] sealed partial class SetUpList {
    public readonly Action applyFilter;
    public readonly ISubscription subscription;
  }
  
  /// <summary>List of all the buttons and it's list control instance.</summary>
  [Record] sealed partial class BoundButtonList {
    public readonly ImmutableList<ButtonBinding> buttons;
    public readonly SetUpList list;
  }

  [Record] sealed partial class SelectedGroup {
    public readonly ButtonBinding groupButton;
    public readonly ImmutableList<ButtonBinding> commandButtons;
  }
}
