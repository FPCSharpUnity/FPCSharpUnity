using System;
using FPCSharpUnity.core.functional;
using GenerationAttributes;

namespace FPCSharpUnity.unity.debug;

public partial class StateExposer {
  public interface IData {
    /// <summary>Does this data represent statically accessible values?</summary>
    bool isStatic { get; }
    Option<ForRepresentation> representation { get; }
  }

  /// <summary>Value that is available an object instance.</summary>
  [Record] partial class InstanceData<A> : IData where A : class {
    readonly WeakReference<A> reference;
    readonly string name;
    readonly Func<A, RenderableValue> get;

    public bool isStatic => false;

    public Option<ForRepresentation> representation => 
      reference.TryGetTarget(out var _ref)
        ? Some.a(new ForRepresentation(Some.a<object>(_ref), name, get(_ref)))
        : None._;
  }

  /// <summary>Value that is available statically.</summary>
  [Record] partial class StaticData : IData {
    readonly string name;
    readonly Func<RenderableValue> get;

    public bool isStatic => true;
    public Option<ForRepresentation> representation => Some.a(new ForRepresentation(None._, name, get()));
  }
}