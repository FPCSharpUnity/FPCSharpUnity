using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.validations;
using GenerationAttributes;
using JetBrains.Annotations;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.functional;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities.Editor {
  public partial class ObjectValidator {
    /// <summary>
    /// Caches structure of our code so that we wouldn't have to constantly use reflection.
    /// </summary>
    public sealed partial class StructureCache {
      // Implementation notes:
      //
      // When adding to concurrent dictionaries we ignore failures because they just mean that some other thread
      // already did the work and our work can be ignored.
      //
      // [LazyProperty] should also be safe, because the worst that can happen is that two threads will calculate
      // the same value because our properties are pure functions.
      
      public static readonly StructureCache defaultInstance = new StructureCache(
        getFieldsForType: (type, cache) => 
          type.type.getAllFields().Select(fi => new Field(fi, cache)).toImmutableArrayC()
      );

      public delegate ImmutableArrayC<Field> GetFieldsForType(Type type, StructureCache cache);
      
      readonly GetFieldsForType _getFieldsForType;
      readonly ConcurrentDictionary<System.Type, Type> typeForSystemType = new();
      readonly ConcurrentDictionary<Type, ImmutableArrayC<Field>> fieldsForType = new();

      public StructureCache(GetFieldsForType getFieldsForType) => _getFieldsForType = getFieldsForType;

      public Type getTypeFor(System.Type systemType) {
        if (!typeForSystemType.TryGetValue(systemType, out var t)) {
          t = new Type(systemType);
          typeForSystemType.TryAdd(systemType, t);
        }

        return t;
      }

      public ImmutableArrayC<Field> getFieldsFor<A>(A a) => 
        a == null ? ImmutableArrayC<Field>.empty : getFieldsForType(a.GetType());

      public ImmutableArrayC<Field> getFieldsForType(System.Type type) => 
        getFieldsForType(getTypeFor(type));
      
      public ImmutableArrayC<Field> getFieldsForType(Type type) {
        if (!fieldsForType.TryGetValue(type, out var fields)) {
          fields = _getFieldsForType(type, this);
          fieldsForType.TryAdd(type, fields);
        }

        return fields;
      }
      
      public Type getListItemType(IList list) {
        var type = getTypeFor(list.GetType());
        if (type.firstGenericTypeArgument.valueOut(out var genericType)) {
          return getTypeFor(genericType);
        }
        if (type.arrayElementType.valueOut(out var arrayElementType)) {
          return getTypeFor(arrayElementType);
        }
        throw new Exception($"Could not determine IList element type for {type.type.FullName}");
      }

      [Record] public sealed partial class Type {
        public readonly System.Type type;
        
        [LazyProperty] public bool hasSerializableAttribute => type.hasAttribute<SerializableAttribute>();
        [LazyProperty] public bool isUnityObject => unityObjectType.IsAssignableFrom(type);
        
        [LazyProperty] public bool isArray => type.IsArray;
        [LazyProperty] public Option<System.Type> arrayElementType => 
          isArray ? Some.a(type.GetElementType()) : Option<System.Type>.None;
        
        [LazyProperty] public bool isGeneric => type.IsGenericType;
        [LazyProperty] public Option<System.Type> firstGenericTypeArgument => 
          isGeneric ? Some.a(type.GenericTypeArguments[0]) : Option<System.Type>.None;
        
        [LazyProperty] public bool isSerializableAsValue =>
          type.IsPrimitive 
          || type == typeof(string)
          || (
            hasSerializableAttribute
            // sometimes serializable attribute is added on ScriptableObject, we want to skip that
            && !isUnityObject
          );

        static readonly System.Type unityObjectType = typeof(UnityEngine.Object);
      }
      
      [Record(GenerateConstructor = ConstructorFlags.None)] public sealed partial class Field {
        public readonly Type type;
        public readonly FieldInfo fieldInfo;

        public Field(FieldInfo fieldInfo, StructureCache cache) {
          this.fieldInfo = fieldInfo;
          type = cache.getTypeFor(fieldInfo.FieldType);
        }

        /// <summary>
        /// <see cref="NotNullAttribute"/> is contained in UnityEngine.CoreModule.dll.
        /// But external dlls can't reference that. They reference the class contained in JetBrains.Annotations.dll.
        /// This code finds reference to `NotNull` type that is not contained in the UnityEngine.CoreModule.dll.
        /// </summary>
        [LazyProperty] public Option<System.Type> additionalNotNullAttributeType { get {
          var nameToFind = typeof(NotNullAttribute).FullName;
          foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (assembly == typeof(NotNullAttribute).Assembly) continue;
            foreach (var type in assembly.ExportedTypes) {
              if (type.FullName == nameToFind) {
                return Some.a(type);
              }
            }
          }
          return None._;
        } }

        
        [LazyProperty] public bool hasNonEmptyAttribute => fieldInfo.hasAttribute<NonEmptyAttribute>();
        [LazyProperty] public bool hasNotNullAttribute =>
          fieldInfo.hasAttribute<NotNullAttribute>() 
          // SerializeReference implies notnull
          || fieldInfo.hasAttribute<SerializeReference>()
          || additionalNotNullAttributeType.valueOut(out var notNullType)
             && fieldInfo.CustomAttributes.Any(notNullType, static (_, notNullType_) => _.AttributeType == notNullType_);
        
        [LazyProperty] public bool hasSerializeReferenceAttribute => fieldInfo.hasAttribute<SerializeReference>();

        [LazyProperty] public ImmutableArrayC<UniqueValue> uniqueValueAttributes => 
          fieldInfo.getAttributes<UniqueValue>().toImmutableArrayC();

        [LazyProperty] public ImmutableArrayC<UnityTagAttribute> unityTagAttributes => 
          fieldInfo.getAttributes<UnityTagAttribute>().toImmutableArrayC();

        [LazyProperty] public ImmutableArrayC<ValidateInputAttribute> validateInputAttributes =>
          fieldInfo.getAttributes<ValidateInputAttribute>().toImmutableArrayC();

        [LazyProperty] public bool isSerializable => fieldInfo.isSerializable();
        [LazyProperty] public bool isSerializableAsReference =>
          isSerializable && fieldInfo.hasAttribute<SerializeReference>();
      }
    }
  }
}