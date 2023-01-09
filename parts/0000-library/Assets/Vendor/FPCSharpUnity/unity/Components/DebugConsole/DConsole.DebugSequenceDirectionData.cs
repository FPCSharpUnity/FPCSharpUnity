using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Data;
using GenerationAttributes;

namespace FPCSharpUnity.unity.Components.DebugConsole;

public partial class DConsole {
  [LazyProperty] public static DebugSequenceDirectionData DEFAULT_DIRECTION_DATA => new DebugSequenceDirectionData();
  
  [LazyProperty] public static ImmutableList<Direction> DEFAULT_DIRECTION_SEQUENCE =>
    ImmutableList.Create(
      Direction.Left, Direction.Right,
      Direction.Left, Direction.Right,
      Direction.Left, Direction.Up,
      Direction.Right, Direction.Down,
      Direction.Right, Direction.Up
    );
  
  public class DebugSequenceDirectionData {
    public readonly string horizontalAxisName, verticalAxisName;
    public readonly Duration timeframe;
    public readonly ImmutableList<Direction> sequence;

    public DebugSequenceDirectionData(
      string horizontalAxisName="Horizontal",
      string verticalAxisName="Vertical",
      TimeSpan timeframe=default,
      ImmutableList<Direction> sequence=null
    ) {
      this.horizontalAxisName = horizontalAxisName;
      this.verticalAxisName = verticalAxisName;
      this.timeframe = timeframe == default ? 5.seconds() : timeframe;
      sequence ??= DEFAULT_DIRECTION_SEQUENCE;
      this.sequence = sequence;

      for (var idx = 0; idx < sequence.Count - 1; idx++) {
        var current = sequence[idx];
        var next = sequence[idx + 1];
        if (current == next) throw new ArgumentException(
          $"{nameof(DebugSequenceDirectionData)} sequence can't contain subsequent elements! " +
          $"Found {current} at {idx} & {idx + 1}.",
          nameof(sequence)
        );
      }
    }
  }
}