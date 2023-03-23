using System.Collections.Generic;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.log;
using FPCSharpUnity.core.reactive;
using FPCSharpUnity.unity.Data;
using static FPCSharpUnity.core.typeclasses.Str;

namespace FPCSharpUnity.unity.Components.DebugConsole; 

public partial class DConsole {
  /// <summary>
  /// Holds the currently available commands for the currently shown view instance.
  /// </summary>
  public class Commands {
    public readonly Dictionary<GroupName, List<Command>> dictionary = new();

    /// <summary>
    /// Creates a <see cref="DConsoleRegistrar"/> for the specified command group.
    /// </summary>
    /// <param name="commandGroupName">See <see cref="DConsoleRegistrar.commandGroup"/>.</param>
    public DConsoleRegistrar registrarFor(string commandGroupName) =>
      new DConsoleRegistrar(this, new GroupName(commandGroupName));
    
    /// <summary>
    /// Registers the command to be shown between commands for the current view.
    /// <para/>
    /// The registrations are killed when the current view is destroyed.
    /// </summary>
    /// <param name="command"></param>
    /// <returns>Subscription that unregisters the <see cref="Command"/> when disposed.</returns>
    public ISubscription register(Command command) {
      command = clearShortcutIfItConflicts();

      var list = dictionary.getOrUpdate(command.cmdGroup, () => new List<Command>());
      list.Add(command);
      var sub = new Subscription(() => list.Remove(command));
      return sub;

      // Drops the shortcut it conflicts are detected.
      Command clearShortcutIfItConflicts() =>
        command.shortcut.foldM(
          command,
          shortcut => checkShortcutForDuplication(shortcut) ? command.withShortcut(None._) : command
        );

      // Returns true if this shortcut clashes with some other shortcut.
      bool checkShortcutForDuplication(KeyCodeWithModifiers shortcut) {
        var hasConflicts = false;
        foreach (var (groupName, groupCommands) in dictionary) {
          foreach (var otherCommand in groupCommands) {
            if (
              otherCommand.shortcut.filter(s1 => s1.wouldTriggerOn(shortcut) || shortcut.wouldTriggerOn(s1))
              .valueOut(out var conflictingShortcut)
            ) {
              log.error(
                $"{command.cmdGroup}/{command.name} shortcut {s(shortcut)} " +
                $"conflicts with {groupName}/{otherCommand.name} shortcut {s(conflictingShortcut)}"
              );
              hasConflicts = true;
            }
          }
        }

        return hasConflicts;
      }
    }
  }
}