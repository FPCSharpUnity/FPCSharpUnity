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

  /// <summary>Function that turns an instance of the object into a renderable value.</summary>
  /// <typeparam name="A">Type of the instance.</typeparam>
  /// <typeparam name="Data">Data for the function that was given at the creation time.</typeparam>
  public delegate RenderableValue Render<in A, in Data>(A instance, Data data);

  /// <summary>Value that is available an object instance.</summary>
  [Record] partial class InstanceData<A, Data> : IData where A : class {
    readonly WeakReference<A> reference;
    readonly string name;
    
    /// <summary>Data for the <see cref="render"/> function.</summary>
    readonly Data data;
    
    /// <inheritdoc cref="Render"/>
    readonly Render<A, Data> render;

    public bool isStatic => false;

    public Option<ForRepresentation> representation => 
      reference.TryGetTarget(out var _ref) 
      // Unity Object may be destroyed, but WeakRef could still be valid. Check if Unity object is still alive.
      && !_ref.Equals(null)
        ? Some.a(new ForRepresentation(Some.a<object>(_ref), name, render(_ref, data)))
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