using System;
using FPCSharpUnity.core.functional;
using FPCSharpUnity.unity.caching;
using JetBrains.Annotations;
using FPCSharpUnity.core.reactive;

namespace FPCSharpUnity.unity.Data {
  /// <summary><see cref="PrefVal{A}"/> that can be inspected in editor.</summary>
  public interface InspectablePrefVal {
    [PublicAPI] object valueUntyped { get; set; }
    /// <summary>
    /// Writes <see cref="PrefVal{A}"/> to disk upon calling. Normally Unity saves on
    /// application exit, but you can use this to force flushing data to the disk.
    ///
    /// Beware, this is very slow on some platforms (for example iOS).
    /// </summary>
    [PublicAPI] void save();
  }

  /// <summary>PlayerPrefs backed value.</summary>
  public interface PrefVal<A> : IRxRef<A>, ICachedBlob<A>, InspectablePrefVal {}

  /// <summary>
  /// Allows you to have a dictionary-like interface to <see cref="PrefVal{A}"/>.
  /// </summary>
  [PublicAPI] public interface PrefValDictionary<in Key, Value> {
    /// <summary>Returns true if such <see cref="PrefVal{A}"/> has previously been initialized.</summary>
    bool hasKey(Key key);
    
    /// <summary>Initializes and returns the <see cref="PrefVal{A}"/> for the given <see cref="Key"/>.</summary>
    PrefVal<Value> this[Key key] { get; }
  }

  public static class PrefValDictionaryExts {
    /// <summary>
    /// As <see cref="PrefValDictionary{Key,Value}.this"/>, but only returns Some if
    /// <see cref="PrefValDictionary{Key,Value}.hasKey"/> would return true.
    /// </summary>
    public static Option<PrefVal<Value>> get<Key, Value>(this PrefValDictionary<Key, Value> dict, Key key) =>
      dict.hasKey(key) ? Some.a(dict[key]) : None._;
  }

  public static class PrefVal {
    [PublicAPI] public delegate void Base64StorePart(byte[] partData);
    [PublicAPI] public delegate byte[] Base64ReadPart();

    public enum OnDeserializeFailure { ReturnDefault, ThrowException }

    public static readonly PrefValStorage player = new PrefValStorage(PlayerPrefsBackend.instance);
#if UNITY_EDITOR
    public static readonly PrefValStorage editor = new PrefValStorage(EditorPrefsBackend.instance);
#endif

    public static void trySetUntyped<A>(this PrefVal<A> val, object value) {
      if (value is A a)
        val.value = a;
      else
        throw new ArgumentException(
          $"Can't assign {value} (of type {value.GetType()}) to {val} (of type {typeof(A)}!"
        );
    }
  }
}