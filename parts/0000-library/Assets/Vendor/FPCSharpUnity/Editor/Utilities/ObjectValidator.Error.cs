﻿using System;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Data.scenes;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using GenerationAttributes;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Utilities.Editor {
  public static partial class ObjectValidator {
    [Record(ConstructorFlags.None, GenerateToString = false)]
    public partial struct Error {
      public enum Type : byte {
        MissingComponent,
        MissingRequiredComponent,
        MissingReference,
        MissingPrefabAsset,
        NullReference,
        EmptyCollection,
        EmptyString,
        UnityEventInvalidMethod,
        UnityEventInvalid,
        TextFieldBadTag,
        CustomValidation,
        CustomValidationException,
        DuplicateUniqueValue,
        ValidatorBug,
        /// <summary>Unity failed to import the file.</summary>
        AssetCorrupted
      }

      /// <summary>Location could not be determined.</summary>
      public struct UnknownLocation : IEquatable<UnknownLocation> {
        public bool Equals(UnknownLocation other) => true;
      }

      public readonly Type type;
      public readonly string message;
      public readonly Object obj;
      /// <summary>Path of the Unity GameObject tree.</summary>
      ///
      /// <example>
      /// Player/Particles/Spawn
      /// </example>
      public readonly string objFullPath;
      public readonly OneOf<AssetPath, ScenePath, UnknownLocation> location;

      public override string ToString() =>
        $"{nameof(Error)}[" +
        $"{type} " +
        $"in '{objFullPath}' " +
        $@"@ '{location.fold(
          asset => asset.path,
          scenePath => scenePath.path,
          unknownLocation => "Unknown location"
        )}'. " +
        $"{message}" +
        $"]";

      #region Constructors

      public Error(Type type, string message, Object obj) : this(
        type, message, obj, fullPath(obj), findLocation(obj)
      ) {}

      public Error(
        Type type, string message, Object obj, string objFullPath, OneOf<AssetPath, ScenePath, UnknownLocation> location
      ) {
        this.type = type;
        this.message = message;
        this.obj = obj;
        this.objFullPath = objFullPath;
        this.location = location;
      }

      static OneOf<AssetPath, ScenePath, UnknownLocation> findLocation(Object obj) {
        foreach (var _ in lookupAssetPath(obj)) return _;
        foreach (var _ in lookupScenePath(obj)) return _;
        // Objects created in Editor Tests don't have a real scene attached
        return new UnknownLocation();
      }

      static Option<AssetPath> lookupAssetPath(Object o) =>
        AssetDatabase.GetAssetPath(o).nonEmptyOpt().map(_ => new AssetPath(_));

      static Option<ScenePath> lookupScenePath(Object o) =>
        from go in F.opt(o as GameObject) || F.opt(o as Component).map(c => c.gameObject)
        from path in go.scene.path.nonEmptyOpt(trim: true)
        select new ScenePath(path);

      // Missing component is null, that is why we need GO
      public static Error missingComponent(GameObject o) => new Error(
        Type.MissingComponent,
        "in GO",
        o
      );

      public static Error emptyCollection(
        Object o, FieldHierarchyStr hierarchy, CheckContext context
      ) => new Error(
        Type.EmptyCollection,
        $"{context}. Property: {hierarchy.s}",
        o
      );

      public static Error emptyString(
        Object o, FieldHierarchyStr hierarchy, CheckContext context
      ) => new Error(
        Type.EmptyString,
        $"{context}. Property: {hierarchy.s}",
        o
      );

      public static Error missingReference(
        Object o, string property, CheckContext context
      ) => new Error(
        Type.MissingReference,
        $"{context}. Property: {property}",
        o
      );

      public static Error requiredComponentMissing(
        GameObject go, System.Type requiredType, System.Type requiredBy, CheckContext context
      ) => new Error(
        Type.MissingRequiredComponent,
        $"{context}. {requiredType} missing (required by {requiredBy})",
        go
      );

      public static Error nullReference(
        Object o, FieldHierarchyStr hierarchy, CheckContext context
      ) => new Error(
        Type.NullReference,
        $"{context}. Property: {hierarchy.s}",
        o
      );

      static string unityEventMessagePrefix(string property, int index) =>
        $"In property '{property}' callback at index {index} of UnityEvent";
      static string unityEventMessageSuffix(CheckContext context) =>
        $"in context '{context}'.";

      public static Error unityEventInvalidMethod(
        Object o, FieldHierarchyStr hierarchy, int index, CheckContext context
      ) => new Error(
        Type.UnityEventInvalidMethod,
        $"{unityEventMessagePrefix(hierarchy.s, index)} has invalid method " +
          unityEventMessageSuffix(context),
        o
      );

      public static Error unityEventInvalid(
        Object o, FieldHierarchyStr hierarchy, int index, CheckContext context
      ) => new Error(
        Type.UnityEventInvalid,
        $"{unityEventMessagePrefix(hierarchy.s, index)} is not valid " +
          unityEventMessageSuffix(context),
        o
      );

      public static Error badTextFieldTag(
        Object o, FieldHierarchyStr hierarchy, CheckContext context
      ) => new Error(
        Type.TextFieldBadTag,
        $"{context}. Property: {hierarchy.s}",
        o
      );

      public static Error customError(
        Object o, FieldHierarchyStr hierarchy, ErrorMsg error, CheckContext context, bool useErrorMessageContext
      ) => new Error(
        Type.CustomValidation,
        $"{context}. Property: {hierarchy.s}. Error: {error}",
        useErrorMessageContext ? error.context.flatMapUnity(o1 => o1 as Object).getOrElse(o) : o
      );
      
      public static Error duplicateUniqueValueError(
        string category, object fieldValue, Object checkedObject, CheckContext context
      ) => new Error(
        Type.DuplicateUniqueValue,
        $"{context}. Duplicate value '{fieldValue}' in category '{category}'",
        checkedObject
      );      

      public static Error customValidationException(
        Object o, FieldHierarchyStr hierarchy, Exception exception, CheckContext context
      ) => new Error(
        Type.CustomValidationException,
        $"{context}. Property: {hierarchy.s}. Error while running {nameof(OnObjectValidate)}:\n{exception}",
        o
      );

      #endregion
    }
  }
}