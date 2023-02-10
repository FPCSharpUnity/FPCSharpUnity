using System;
using System.Collections.Generic;
using System.Globalization;
using ExhaustiveMatching;
using FPCSharpUnity.core.functional;
using GenerationAttributes;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  /// <summary>Represents a value that we can render.</summary>
  [Closed(
    typeof(StringValue), typeof(FloatValue), typeof(BoolValue), typeof(ObjectValue), typeof(ActionValue), 
    typeof(KVValue), typeof(HeaderValue), typeof(EnumerableValue)
  )] public abstract class RenderableValue {
    public static implicit operator RenderableValue(string v) => new StringValue(v);
    public static implicit operator RenderableValue(float v) => new FloatValue(v);
    public static implicit operator RenderableValue(double v) => new FloatValue((float) v);
    public static implicit operator RenderableValue(bool v) => new BoolValue(v);
    public static implicit operator RenderableValue(UnityEngine.Object v) => new ObjectValue(v);
    public static implicit operator RenderableValue(Action v) => new ActionValue(v);
    public static implicit operator RenderableValue(RenderableValue[] v) => new EnumerableValue(v);
    
    /// <summary>Returns `Some` if this can be turned into a string.</summary>
    public abstract Option<string> asString { get; }
  }

  [Record] public sealed partial class StringValue : RenderableValue { 
    public readonly string value;

    public override Option<string> asString => Some.a(value);
  }

  [Record] public sealed partial class FloatValue : RenderableValue {
    public readonly float value;

    public override Option<string> asString => Some.a(value.ToString(CultureInfo.InvariantCulture));
  }

  [Record] public sealed partial class BoolValue : RenderableValue {
    public readonly bool value;

    public override Option<string> asString => Some.a(value.ToString());
  }

  [Record] public sealed partial class ObjectValue : RenderableValue {
    public readonly UnityEngine.Object value;

    public override Option<string> asString => None._;
  }
  [Record] public sealed partial class ActionValue : RenderableValue {
    public readonly string label;
    public readonly Action value;

    public ActionValue(Action value) : this("Run", value) {}

    public override Option<string> asString => None._;
  }

  /// <summary>Renders the key in column 1 and value in column 2.</summary>
  [Record] public sealed partial class KVValue : RenderableValue {
    public readonly RenderableValue key, value;

    public override Option<string> asString => None._;
  }
    
  /// <summary>Renders a header and then the value, but indents the value by the specified indent.</summary>
  [Record] public sealed partial class HeaderValue : RenderableValue {
    public readonly RenderableValue header, value;
    public readonly byte indentBy;

    public HeaderValue(RenderableValue header, RenderableValue value) : this(header, value, 1) {}

    public override Option<string> asString => None._;
  }
    
  /// <summary>Renders given values with header stating the count and separator between values.</summary>
  [Record] public sealed partial class EnumerableValue : RenderableValue {
    /// <summary>Should we render the element count?</summary>
    public readonly bool showCount;
    
    public readonly IReadOnlyCollection<RenderableValue> values;

    public EnumerableValue(IReadOnlyCollection<RenderableValue> values) : this(showCount: true, values) {}

    public override Option<string> asString => None._;
  }
}