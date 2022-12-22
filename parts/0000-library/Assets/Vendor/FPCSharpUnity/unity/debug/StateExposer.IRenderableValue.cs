using System;
using System.Collections.Generic;
using System.Globalization;
using FPCSharpUnity.core.functional;
using GenerationAttributes;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  /// <summary>Represents a value that we can render.</summary>
  [Matcher] public abstract class IRenderableValue {
    public static implicit operator IRenderableValue(string v) => new StringValue(v);
    public static implicit operator IRenderableValue(float v) => new FloatValue(v);
    public static implicit operator IRenderableValue(double v) => new FloatValue((float) v);
    public static implicit operator IRenderableValue(bool v) => new BoolValue(v);
    public static implicit operator IRenderableValue(UnityEngine.Object v) => new ObjectValue(v);
    public static implicit operator IRenderableValue(Action v) => new ActionValue(v);
    public static implicit operator IRenderableValue(IRenderableValue[] v) => new EnumerableValue(v);
    
    /// <summary>Returns `Some` if this can be turned into a string.</summary>
    public abstract Option<string> asString { get; }
  }

  [Record] public sealed partial class StringValue : IRenderableValue { 
    public readonly string value;

    public override Option<string> asString => Some.a(value);
  }

  [Record] public sealed partial class FloatValue : IRenderableValue {
    public readonly float value;

    public override Option<string> asString => Some.a(value.ToString(CultureInfo.InvariantCulture));
  }

  [Record] public sealed partial class BoolValue : IRenderableValue {
    public readonly bool value;

    public override Option<string> asString => Some.a(value.ToString());
  }

  [Record] public sealed partial class ObjectValue : IRenderableValue {
    public readonly UnityEngine.Object value;

    public override Option<string> asString => None._;
  }
  [Record] public sealed partial class ActionValue : IRenderableValue {
    public readonly string label;
    public readonly Action value;

    public ActionValue(Action value) : this("Run", value) {}

    public override Option<string> asString => None._;
  }

  /// <summary>Renders the key in column 1 and value in column 2.</summary>
  [Record] public sealed partial class KVValue : IRenderableValue {
    public readonly IRenderableValue key, value;

    public override Option<string> asString => None._;
  }
    
  /// <summary>Renders a header and then the value, but indents the value by the specified indent.</summary>
  [Record] public sealed partial class HeaderValue : IRenderableValue {
    public readonly IRenderableValue header, value;
    public readonly byte indentBy;

    public HeaderValue(IRenderableValue header, IRenderableValue value) : this(header, value, 1) {}

    public override Option<string> asString => None._;
  }
    
  /// <summary>Renders given values with header stating the count and separator between values.</summary>
  [Record] public sealed partial class EnumerableValue : IRenderableValue {
    /// <summary>Should we render the element count?</summary>
    public readonly bool showCount;
    public readonly IReadOnlyCollection<IRenderableValue> values;

    public EnumerableValue(IReadOnlyCollection<IRenderableValue> values) : this(showCount: true, values) {}

    public override Option<string> asString => None._;
  }
}