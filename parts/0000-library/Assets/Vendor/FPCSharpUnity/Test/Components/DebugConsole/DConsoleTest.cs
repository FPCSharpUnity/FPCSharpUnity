using System;
using System.Collections.Immutable;
using FPCSharpUnity.unity.Components.DebugConsole;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Test.Components.DebugConsole {
  public class DebugSequenceDirectionTestSequenceValidation {
    [Test]
    public void WhenGood() {
      var _ = new DConsole.DebugSequenceDirectionData(sequence: ImmutableList.Create(
        DConsole.Direction.Left, DConsole.Direction.Up,
        DConsole.Direction.Down, DConsole.Direction.Right
      ));
    }

    [Test]
    public void WhenBadInStart() {
      Assert.Throws<ArgumentException>(() => new DConsole.DebugSequenceDirectionData(sequence: ImmutableList.Create(
        DConsole.Direction.Left, DConsole.Direction.Left,
        DConsole.Direction.Up, DConsole.Direction.Down, DConsole.Direction.Right
      )));
    }

    [Test]
    public void WhenBadInEnd() {
      Assert.Throws<ArgumentException>(() => new DConsole.DebugSequenceDirectionData(sequence: ImmutableList.Create(
        DConsole.Direction.Left, DConsole.Direction.Up, DConsole.Direction.Down,
        DConsole.Direction.Right, DConsole.Direction.Right
      )));
    }

    [Test]
    public void WhenSingleElement() {
      new DConsole.DebugSequenceDirectionData(sequence: ImmutableList.Create(
        DConsole.Direction.Left
      ));
    }
  }
}