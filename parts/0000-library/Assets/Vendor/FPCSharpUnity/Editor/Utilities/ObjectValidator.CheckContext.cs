using System;
using System.Collections.Immutable;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Utilities.Editor {
  public static partial class ObjectValidator {
    public class CheckContext {
      public static readonly CheckContext empty =
        new CheckContext(None._, ImmutableHashSet<Type>.Empty);

      public readonly Option<string> value;
      public readonly ImmutableHashSet<Type> checkedComponentTypes;

      public CheckContext(Option<string> value, ImmutableHashSet<Type> checkedComponentTypes) {
        this.value = value;
        this.checkedComponentTypes = checkedComponentTypes;
      }

      public CheckContext(string value) : this(Some.a(value), ImmutableHashSet<Type>.Empty) {}

      public override string ToString() => value.getOrElse("unknown ctx");

      public CheckContext withCheckedComponentType(Type c) =>
        new CheckContext(value, checkedComponentTypes.Add(c));
    }
  }
}