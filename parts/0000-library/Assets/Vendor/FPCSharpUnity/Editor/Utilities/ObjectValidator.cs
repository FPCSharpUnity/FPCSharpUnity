﻿// From my experiments profiling doesn't add any overhead but it might have issues with multithreading so it is
// turned off by default.
//#define DO_PROFILE
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Extensions;
using FPCSharpUnity.core.exts;
using UnityEngine.Events;
using JetBrains.Annotations;
using FPCSharpUnity.unity.Filesystem;
using FPCSharpUnity.unity.Functional;
using FPCSharpUnity.unity.Logger;
using FPCSharpUnity.core.log;
using GenerationAttributes;
using FPCSharpUnity.core.collection;
 using FPCSharpUnity.core.data;
 using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.utils;
using FPCSharpUnity.unity.core.Utilities;
using UnityEngine.Playables;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace FPCSharpUnity.unity.Utilities.Editor {
  public static partial class ObjectValidator {
    public delegate void AddError(Func<Error> createError);
    
    public static OnProgress createOnProgress() {
      const float TIME_DIFF = 1f / 30;
      var nextProgressAt = 0f;
      return progress => {
        // calling DisplayProgressBar too often affects performance
        var currentTime = Time.realtimeSinceStartup;
        if (currentTime >= nextProgressAt) {
          nextProgressAt = currentTime + TIME_DIFF;
          var cancelled = EditorUtility.DisplayCancelableProgressBar(
            "Validating Objects", progress.text, progress.ratio
          );
          return cancelled;
        }
        else {
          return false;
        }
      };
    }

    public static OnProgress DEFAULT_ON_PROGRESS => createOnProgress();
    public static readonly Action DEFAULT_ON_FINISH = EditorUtility.ClearProgressBar;
    
    #region Menu Items
    
    [UsedImplicitly, MenuItem(
      "Tools/FP C# Unity/Validate Objects in Current Scene",
      isValidateFunction: false, priority: 55
    )]
    static void checkCurrentSceneMenuItem() {
      if (EditorApplication.isPlayingOrWillChangePlaymode) {
        EditorUtility.DisplayDialog(
          "In Play Mode!",
          "This action cannot be run in play mode. Aborting!",
          "OK"
        );
        return;
      }

      var scene = SceneManagerBetter.instance.getActiveScene();
      var (errors, timeSpan) = checkSceneWithTime(scene, None._, DEFAULT_ON_PROGRESS, DEFAULT_ON_FINISH);
      showErrors(Log.d, errors);
      if (Log.d.isInfo()) Log.d.info(
        $"{scene.name} {nameof(checkCurrentSceneMenuItem)} finished in {timeSpan}"
      );
    }

    [UsedImplicitly, MenuItem(
       "Tools/FP C# Unity/Validate Selected Objects",
       isValidateFunction: false, priority: 56
     )]
    static void checkSelectedObjects() {
      var errors = check(
        new CheckContext("Selection"), Selection.objects,
        None._,
        onProgress: progress => EditorUtility.DisplayCancelableProgressBar(
          "Validating Objects", "Please wait...", progress.ratio
        ),
        onFinish: EditorUtility.ClearProgressBar, 
        uniqueValuesCache: UniqueValuesCache.create.some()
      );
      showErrors(Log.d, errors);
    }
    
    #endregion
    
    [PublicAPI]
    public static void showErrors(
      ILog log, IEnumerable<Error> errors, LogLevel level = LogLevel.ERROR
    ) {
      if (!log.willLog(level)) return;
      
      foreach (var error in errors) {
        // If context is a MonoBehaviour,
        // then unity does not ping the object (only a folder) when clicked on the log message.
        // But it works fine for GameObjects
        var maybeGo = getGameObject(error.obj);
        var context = maybeGo.valueOut(out var go) && go.scene.name == null
          ? getRootGO(go) // get root GameObject on prefabs
          : error.obj;
        log.log(level, LogEntry.simple(error.ToString(), context: context));

        static GameObject getRootGO(GameObject go) {
          var t = go.transform;
          while (true) {
            if (t.parent == null) return t.gameObject;
            t = t.parent;
          }
        }

        static Option<GameObject> getGameObject(Object obj) {
          if (!obj) return None._;
          return obj switch {
            GameObject go => go.some(),
            Component c => F.opt(c.gameObject),
            _ => None._
          };
        }
      }
    }
    
    /// <summary>
    /// Collect all objects that are needed to create given roots. 
    /// </summary>
    [PublicAPI]
    public static ImmutableArray<Object> collectDependencies(Object[] roots) =>
      roots.isEmpty() 
        ? ImmutableArray<Object>.Empty 
        : EditorUtility.CollectDependencies(roots)
          .Where(o => o is GameObject or ScriptableObject)
          .Distinct()
          .ToImmutableArray();

    /// <summary>Invoked to report the progress.</summary>
    /// <returns>True if we should abort the validation.</returns>
    public delegate bool OnProgress(Progress progress);
    
    [PublicAPI]
    public static ImmutableList<Error> checkScene(
      Scene scene, Option<CustomObjectValidator> customValidatorOpt = default,
      OnProgress onProgress = null, Action onFinish = null
    ) {
      var objects = getSceneObjects(scene);
      var errors = check(
        new CheckContext(scene.name), objects, customValidatorOpt, 
        onProgress: onProgress, onFinish: onFinish
      );
      return errors;
    }

    [PublicAPI]
    public static Tpl<ImmutableList<Error>, TimeSpan> checkSceneWithTime(
      Scene scene, Option<CustomObjectValidator> customValidatorOpt = default,
      OnProgress onProgress = null, Action onFinish = null
    ) {
      var stopwatch = Stopwatch.StartNew();
      var errors = checkScene(
        scene, customValidatorOpt, 
        onProgress: onProgress, onFinish: onFinish
      );
      return Tpl.a(errors, stopwatch.Elapsed);
    }

    [PublicAPI]
    public static ImmutableList<Error> checkAssetsAndDependencies(
      IEnumerable<PathStr> assets, Option<CustomObjectValidator> customValidatorOpt = default,
      OnProgress onProgress = null, Action onFinish = null
    ) {
      var loadedAssets =
        assets.Select(s => AssetDatabase.LoadMainAssetAtPath(s)).ToArray();
      var dependencies = collectDependencies(loadedAssets);
      return check(
        // and instead of &, because unity does not show '&' in some windows
        new CheckContext("Assets and Deps"),
        dependencies, customValidatorOpt, 
        onProgress: onProgress, onFinish: onFinish, 
        uniqueValuesCache: UniqueValuesCache.create.some()
      );
    }

    /// <summary>
    /// Check objects and their children.
    /// 
    /// <see cref="check"/>.
    /// </summary>
    [PublicAPI]
    public static ImmutableList<Error> checkRecursively(
      CheckContext context, IEnumerable<Object> objects,
      Option<CustomObjectValidator> customValidatorOpt = default,
      OnProgress onProgress = null, Action onFinish = null
    ) => check(
      context,
      collectDependencies(objects.ToArray()),
      customValidatorOpt: customValidatorOpt, onProgress: onProgress, onFinish: onFinish,
      uniqueValuesCache: UniqueValuesCache.create.some()
    );

    /// <summary>
    /// Check given objects. This does not walk through them. <see cref="checkRecursively"/>.
    /// </summary>
    [PublicAPI]
    public static ImmutableList<Error> check(
      CheckContext context, ICollection<Object> objects,
      Option<CustomObjectValidator> customValidatorOpt = default,
      OnProgress onProgress = null,
      Action onFinish = null,
      Option<UniqueValuesCache> uniqueValuesCache = default
    ) {
      Option.ensureValue(ref customValidatorOpt);
      Option.ensureValue(ref uniqueValuesCache);

      var jobController = new JobController();
      var errors = new List<Error>();
      // Creating errors might involve Unity API thus we defer it to main thread.
      void addError(Func<Error> e) => jobController.enqueueMainThreadJob(() => errors.Add(e()));
      
      var structureCache = StructureCache.defaultInstance;
      var unityTags = UnityEditorInternal.InternalEditorUtility.tags.ToImmutableHashSet();
      var scanned = 0;
      
      var distinctObjects = objects.Distinct().toImmutableArrayC();

      validateMissingPrefabAssets();

      var componentsDuplicated = distinctObjects.SelectMany(o => {
        if (o is GameObject go) {
          return go.transform
            .andAllChildrenRecursive()
            .SelectMany(transform => transform.GetComponents<Component>().collect(c => {
              if (c) return Some.a((Object) c);
              else errors.Add(Error.missingComponent(transform.gameObject));
              return None._;
            }))
            .ToArray();
        }
        else {
          return new[] { o };
        }
      }).ToArray();

      var components = componentsDuplicated.Distinct().ToArray();
      
      // Debug.Log($"All components: {componentsDuplicated.Length} distinct: {components.Length}");

      // It runs faster if we put everything on one thread
      // Increased performance when validating all prefabs: 26s -> 19s
      // single thread of this runs faster than checkComponentMainThreadPart
      // checkComponentMainThreadPart completes in 16s
      var t = new Thread(() => {
        try {
          foreach (var component in components) {
            checkComponentThreadSafePart(
              context: context, component: component, customObjectValidatorOpt: customValidatorOpt, addError: addError,
              structureCache: structureCache, jobController: jobController, unityTags: unityTags,
              uniqueCache: uniqueValuesCache
            );
          }
        }
        catch (Exception e) {
          Debug.LogError("Exception in object validator");
          Debug.LogException(e);
          addError(() => new Error(Error.Type.ValidatorBug, e.Message, null));
        }
      });
      t.Start();
      
      foreach (var component in components) {
        var componentCancelled = processComponent(component);
        if (componentCancelled) break;
      }
      
      var cancelled =
        onProgress
          ?.Invoke(new Progress(components.Length, components.Length, () => jobController.ToString()))
        ?? false;
      
      // Wait till jobs are completed
      while (!cancelled) {
        progressFrequent();
        var action = jobController.serviceMainThread(launchUnderBatchSize: true);
        if (action == JobController.MainThreadAction.RerunAfterDelay) {
          Thread.Sleep(10);
        }
        else if (action == JobController.MainThreadAction.RerunImmediately) {
          // do nothing
        }
        else if (action == JobController.MainThreadAction.Halt) {
          progressFrequent();
          if (t.IsAlive) {
            Thread.Sleep(10);
          }
          else {
            break;
          }
        }
        else {
          throw new Exception($"Unknown value {action}");
        }

        void progressFrequent() {
          cancelled = onProgress?.Invoke(new Progress(
            jobController.jobsDone.toIntClamped(),
            jobController.jobsMax.toIntClamped(), 
            () => $"[{activeThreads("Loop")}] {jobController}"
          )) ?? false;
        }
      }

      // Returns whether user pressed cancel. 
      bool processComponent(Object component) {
        var cancelled =
          onProgress
            ?.Invoke(new Progress(scanned++, components.Length, () => $"[{activeThreads("Main")}] {jobController}"))
          ?? false;
        if (cancelled) return true;
        checkComponentMainThreadPart(context, component, addError, structureCache);
        return false;
      }

      string activeThreads(string currentName) => 
        t.IsAlive ? currentName + ", Thread" : currentName;

      var exceptionCount = jobController.jobExceptions.Count;
      if (exceptionCount != 0) {
        throw new AggregateException(
          $"Job controller had {exceptionCount} exceptions, returning max 25!",
          jobController.jobExceptions.Take(25)
        );
      }

      foreach (var valuesCache in uniqueValuesCache) {
        foreach (var df in valuesCache.getDuplicateFields())
          foreach (var obj in df.objectsWithThisValue) {
            errors.Add(Error.duplicateUniqueValueError(df.category, df.fieldValue, obj, context));
          }
      }

      onFinish?.Invoke();

      // FIXME: there should not be a need to a Distinct call here, we have a bug somewhere in the code.
      return errors.Distinct().ToImmutableList();

      void validateMissingPrefabAssets() {
        using var ps = new ProfiledScope(Macros.classAndMethodNameShort);
          
        foreach (var obj in distinctObjects) {
          switch (obj) {
            case GameObject go:
              validateGO(go);
              break;
          }

          void validateGO(GameObject go) {
            foreach (var transform in go.transform.andAllChildrenRecursive()) {
              if (
                PrefabUtility.IsPrefabAssetMissing(transform)
                // Prefab API does not work as expected on components of prefab assets.
                // It only works in an open scene or in a prefab that is opened in prefab editing mode.
                // But we can still detect this error by checking the name of the GameObject.
                || transform.name.Contains("Missing Prefab with guid:")
              ) {
                errors.Add(new Error(Error.Type.MissingPrefabAsset, $"Missing prefab asset on '{transform.name}'", go));
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Check one component non-recursively. 
    /// </summary>
    static void checkComponentThreadSafePart(
      CheckContext context, Object component, Option<CustomObjectValidator> customObjectValidatorOpt,
      AddError addError, StructureCache structureCache, JobController jobController,
      ImmutableHashSet<string> unityTags,
      Option<UniqueValuesCache> uniqueCache = default
    ) {
      Option.ensureValue(ref uniqueCache);
      var createError = new ErrorFactory(component, context);
      validateFields(
        containingComponent: component,
        objectBeingValidated: component,
        createError: createError,
        addError, structureCache, jobController, unityTags,
        customObjectValidatorOpt: customObjectValidatorOpt,
        uniqueValuesCache: uniqueCache
      );
      
      foreach (var customValidator in customObjectValidatorOpt) {
#if DO_PROFILE
        using (new ProfiledScope(nameof(customValidator)))
#endif
        {
          if (customValidator.isThreadSafe) run();
          else jobController.enqueueMainThreadJob(run);

          void run() {
            var customValidatorErrors =
              customValidator.validateComponent(component).ToArray();
            if (customValidatorErrors.Length > 0) {
              foreach (var error in customValidatorErrors) {
                addError(() => createError.custom(new FieldHierarchyStr(), error, true));
              }
            }
          }
        }
      }
    }

    static void checkComponentMainThreadPart(
      CheckContext context, Object component, AddError addError, StructureCache structureCache
    ) {
      {
        if (component is MonoBehaviour mb)
#if DO_PROFILE
          using (new ProfiledScope(nameof(checkRequireComponents)))
#endif
        {
          var componentType = structureCache.getTypeFor(component.GetType());
          checkRequireComponents(context: context, go: mb.gameObject, type: componentType.type, addError);
          // checkRequireComponents should be called every time
          // if (!context.checkedComponentTypes.Contains(item: componentType)) {
          //   errors = errors.AddRange(items: checkComponentType(context: context, go: mb.gameObject, type: componentType));
          //   context = context.withCheckedComponentType(c: componentType);
          // }
        }
      }

#if DO_PROFILE
      using (new ProfiledScope("Serialized object"))
#endif
      {
        SerializedObject serObj;

#if DO_PROFILE
        using (new ProfiledScope("Create serialized object"))
#endif
        {
          serObj = new SerializedObject(obj: component);
        }

        SerializedProperty sp;

#if DO_PROFILE
        using (new ProfiledScope("Get iterator"))
#endif
        {
          sp = serObj.GetIterator();
        }

        var isPlayableDirector = component is PlayableDirector;

#if DO_PROFILE
        using (new ProfiledScope("Iteration"))
#endif
        {
          while (sp.NextVisible(enterChildren: true)) {
            if (isPlayableDirector && sp.name == "m_SceneBindings") {
              // skip Scene Bindings of PlayableDirector, because they often have missing references
              if (!sp.NextVisible(enterChildren: false)) break;
            }

            if (
              sp.propertyType == SerializedPropertyType.ObjectReference
              && !sp.objectReferenceValue
              && sp.objectReferenceInstanceIDValue != 0
            ) {
              var propertyPathClosure = sp.propertyPath;
              addError(() => Error.missingReference(o: component, property: propertyPathClosure, context: context));
            }
          }
        }
      }
    }

    static IEnumerable<Error> checkUnityEvent(
      IErrorFactory errorFactory, FieldHierarchyStr fieldHierarchy, UnityEventBase evt
    ) {
      UnityEventReflector.rebuildPersistentCallsIfNeeded(evt);

      var persistentCalls = evt.__persistentCalls();
      var listPersistentCallOpt = persistentCalls.calls;
      foreach (var listPersistentCall in listPersistentCallOpt) {
        var index = 0;
        foreach (var persistentCall in listPersistentCall) {
          if (persistentCall.isValid) {
            if (evt.__findMethod(persistentCall).isNone)
              yield return errorFactory.unityEventInvalidMethod(fieldHierarchy, index);
          }
          else
            yield return errorFactory.unityEventInvalid(fieldHierarchy, index);

          index++;
        }
      }
    }

    public readonly struct FieldHierarchyStr {
      public readonly string s;
      public FieldHierarchyStr(string s) { this.s = s; }
      public override string ToString() => $"{nameof(FieldHierarchy)}({s})";
    }

    [Record(GenerateComparer = false)] public sealed partial class FieldHierarchy {
      readonly ImmutableStack<string> stack;
      
      public FieldHierarchy() : this(ImmutableStack<string>.Empty) { }
      
      public FieldHierarchy push(string s) => new FieldHierarchy(stack.Push(s));

      public FieldHierarchyStr asString() => new FieldHierarchyStr(stack.Reverse().mkString('.'));

      public static bool operator ==(FieldHierarchy left, FieldHierarchy right) {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(null, right)) return false;
        if (ReferenceEquals(null, left)) return false;
        return left.stack.structuralEquals() == right.stack.structuralEquals();
      }

      public static bool operator !=(FieldHierarchy left, FieldHierarchy right) => !(left == right);
    }

    static void validateFields(
      Object containingComponent,
      object objectBeingValidated,
      IErrorFactory createError,
      AddError addError,
      StructureCache structureCache,
      JobController jobController,
      ImmutableHashSet<string> unityTags,
      Option<CustomObjectValidator> customObjectValidatorOpt,
      FieldHierarchy fieldHierarchy = null,
      Option<UniqueValuesCache> uniqueValuesCache = default
    ) {
#if DO_PROFILE
      using var _ = new ProfiledScope(nameof(validateFields));
#endif
      Option.ensureValue(ref uniqueValuesCache);
      fieldHierarchy ??= new FieldHierarchy(ImmutableStack<string>.Empty);

      if (objectBeingValidated == null) {
        addError(() => createError.nullField(fieldHierarchy.asString()));
        return;
      }

      if (objectBeingValidated is OnObjectValidate onObjectValidatable) {
#if DO_PROFILE
        using (new ProfiledScope(nameof(OnObjectValidate)))
#endif
        {
          if (onObjectValidatable.onObjectValidateIsThreadSafe) run();
          else jobController.enqueueMainThreadJob(run);

          void run() {
            // Try because custom validations can throw exceptions.
            try {
              var objectErrors = onObjectValidatable.onObjectValidate(containingComponent);
              foreach (var error in objectErrors) {
                addError(() => createError.custom(fieldHierarchy.asString(), error, true));
              }
            }
            catch (Exception error) {
              addError(() => createError.exceptionInCustomValidator(fieldHierarchy.asString(), error));
            }
          }
        }
      }

      {
        if (objectBeingValidated is UnityEventBase unityEvent) {
          // Unity events use unity API
          jobController.enqueueMainThreadJob(() => {
#if DO_PROFILE
            using (new ProfiledScope(nameof(checkUnityEvent)))
#endif
            {
              foreach (var error in checkUnityEvent(createError, fieldHierarchy.asString(), unityEvent)) {
                addError(() => error);
              }
            }
          });
        }
      }

      ImmutableArrayC<StructureCache.Field> fields;
#if DO_PROFILE
      using (new ProfiledScope("get object fields"))
#endif
      {
        fields = structureCache.getFieldsFor(objectBeingValidated);
      }

      ImmutableHashSet<string> blacklistedFields;
#if DO_PROFILE
      using (new ProfiledScope("get blacklisted object fields"))
#endif
      {
        blacklistedFields = 
          objectBeingValidated is ISkipObjectValidationFields svf
          ? svf.blacklistedFields().ToImmutableHashSet()
          : ImmutableHashSet<string>.Empty;
      }

      foreach (var field in fields) {
        validateField(
          containingComponent, objectBeingValidated, createError, addError, structureCache, jobController, unityTags,
          customObjectValidatorOpt, fieldHierarchy, uniqueValuesCache, blacklistedFields, field
        );
      }
    }

    static void validateField(
      Object containingComponent, object objectBeingValidated, IErrorFactory createError,
      AddError addError, StructureCache structureCache, JobController jobController,
      ImmutableHashSet<string> unityTags, Option<CustomObjectValidator> customObjectValidatorOpt,
      FieldHierarchy parentFieldHierarchy, Option<UniqueValuesCache> uniqueValuesCache, 
      ImmutableHashSet<string> blacklistedFields,
      StructureCache.Field field
    ) {
#if DO_PROFILE
      using var _ = new ProfiledScope(nameof(validateField));
#endif
      if (blacklistedFields.Contains(field.fieldInfo.Name)) return;

      var fieldValue = field.fieldInfo.GetValue(objectBeingValidated);
      var fieldHierarchy = parentFieldHierarchy.push(field.fieldInfo.Name);
      
      // todo: mark if it's thread safe
      foreach (var customValidator in customObjectValidatorOpt) {
#if DO_PROFILE
        using (new ProfiledScope(nameof(customValidator)))
#endif
        {
          if (customValidator.isThreadSafe) run();
          else jobController.enqueueMainThreadJob(run);

          void run() {
            try {
              var customValidatorErrors =
                customValidator.validateField(containingComponent, fieldValue, field).ToArray();
              if (customValidatorErrors.Length > 0) {
                var hierarchy = fieldHierarchy.asString();
                foreach (var error in customValidatorErrors) {
                  addError(() => createError.custom(hierarchy, error, true));
                }
              }
            }
            catch (Exception e) {
              addError(() => createError.exceptionInCustomValidator(fieldHierarchy.asString(), e));
            }
          }
        }
      }

      {
        if (uniqueValuesCache.valueOut(out var cache)) {
#if DO_PROFILE
          using (new ProfiledScope(nameof(uniqueValuesCache)))
#endif
          {
            foreach (var attribute in field.uniqueValueAttributes) {
              cache.addCheckedField(attribute.category, fieldValue, containingComponent);
            }
          }
        }
      }
      if (fieldValue is string s) {
#if DO_PROFILE
        using (new ProfiledScope(nameof(field.unityTagAttributes)))
#endif
        {
          var unityTagAttributes = field.unityTagAttributes;
          if (unityTagAttributes.nonEmpty()) {
            if (!unityTags.Contains(s)) {
              addError(() => createError.badTextFieldTag(fieldHierarchy.asString()));
            }
          }
        }

        if (field.hasNonEmptyAttribute && s.isEmpty()) {
          addError(() => createError.emptyString(fieldHierarchy.asString()));
        }

        

        {
          var attributes = field.fieldInfo.getAttributes<ShaderPropertyAttribute>().ToArray();
          if (attributes.Length == 1) {
            // This job is needed to be handled by main thread.
            // Because we're checking from Renderer 'SharedMaterials' which can only be accessed in main thread.
            jobController.enqueueMainThreadJob(() => validateShaderPropertyAttribute(attributes[0]));
          }
        }
        
        void validateShaderPropertyAttribute(ShaderPropertyAttribute attribute) {
          var maybeMethod = objectBeingValidated.GetType().GetMethodInHierarchy(attribute.rendererGetter);
          var maybeValidationError = maybeMethod.flatMap(method => {
            if (method.GetParameters().Length == 0) {
              var invokeReturn = method.Invoke(objectBeingValidated, null);
              var maybeRenderer = invokeReturn.downcast(default(Renderer));
              return ShaderUtilsEditor.validateShaderProperty(
                maybeRenderer, shaderPropertyName: s, attribute.forType
              );
            }
            else {
              return None._;
            }
          });
          
          if (maybeValidationError.valueOut(out var validationError)) {
            addError(() => new Error(
              Error.Type.CustomValidationException,
              validationError.message,
              containingComponent
            ));
          }
        }
      }

      if (field.isSerializable) {
#if DO_PROFILE
        using (new ProfiledScope(nameof(field.isSerializable)))
#endif
        {
          void addNotNullError() => addError(() => createError.nullField(fieldHierarchy.asString()));

          {
            var validateInputAttributes = field.validateInputAttributes;
            foreach (var attribute in validateInputAttributes) {

              var maybeMethod = objectBeingValidated.GetType().GetMethodInHierarchy(attribute.Condition);

              if (!maybeMethod.valueOut(out var method) ) {
                var maybeProperty = objectBeingValidated.GetType().GetPropertyInHierarchy(attribute.Condition);
                if (!maybeProperty.valueOut(out var property)) {
                  addFailedError(
                    $"Validator method or property not found. " +
                    $"Looked for method or property with a name {attribute.Condition} " +
                    $"on type {objectBeingValidated.GetType().FullName}"
                  );
                }
                else {
                  jobController.enqueueMainThreadJob(() => {
                    var succeeded = (bool) property.GetValue(objectBeingValidated);
                    if (!succeeded) {
                      addFailedError($"Custom validation failed with message: {attribute.DefaultMessage}");
                    }
                  });
                }
              }
              else {
                var paramCount = method.GetParameters().Length;
                switch (paramCount) {
                  case 1:
                    jobController.enqueueMainThreadJob(() => {
                      var succeeded = (bool) method.Invoke(objectBeingValidated, new [] {fieldValue});
                      if (!succeeded) {
                        addFailedError($"Custom validation failed with message: {attribute.DefaultMessage}");
                      }
                    });
                    break;
                  case 2: {
                    var arguments = new object[] {fieldValue, null};
                    jobController.enqueueMainThreadJob(() => {
                      var succeeded = (bool) method.Invoke(objectBeingValidated, arguments);
                      if (!succeeded) {
                        var errorMessage = (string) arguments[1];
                        addFailedError($"Custom validation failed with message: {errorMessage}");
                      }
                    });
                    break;
                  }
                  default:
                    addFailedError($"Validation with {paramCount} parameters is not implemented.");
                    break;
                }
              }

              void addFailedError(string errorMsg) {
                addError(() => createError.custom(
                  fieldHierarchy.asString(),
                  new ErrorMsg(errorMsg),
                  useErrorMessageContext: false
                ));
              }
            }
          }

          switch (fieldValue) {
            case null:
              if (field.hasNotNullAttribute) addNotNullError();
              break;
            // Sometimes we get empty unity object.
            case Object unityObj:
              if (field.hasNotNullAttribute) {
                jobController.enqueueMainThreadJob(() => {
                  if (!unityObj) addNotNullError();
                });
              }

              break;
            case IList list: {
              if (list.Count == 0) {
                if (field.hasNonEmptyAttribute) {
                  addError(() => createError.emptyCollection(fieldHierarchy.asString()));
                }
              }
              else {
                validateListElementsFields(
                  containingComponent, list, hasNotNull: field.hasNotNullAttribute,
                  hasSerializeReference: field.hasSerializeReferenceAttribute,
                  fieldHierarchy, createError, addError, structureCache, jobController, 
                  unityTags, customObjectValidatorOpt, uniqueValuesCache
                );
              }

              break;
            }
            default: {
              if (field.type.isSerializableAsValue || field.isSerializableAsReference) {
                validateFields(
                  containingComponent, fieldValue, createError, addError, structureCache, jobController, 
                  unityTags, customObjectValidatorOpt, fieldHierarchy, uniqueValuesCache
                );
              }
              
              if (field.type.type.IsEnum && !field.type.type.hasAttribute<FlagsAttribute>()) {
                if (!field.type.type.IsEnumDefined(fieldValue)) {
                  addError(() => createError.custom(
                    fieldHierarchy.asString(),
                    new ErrorMsg($"Invalid enum value of '{fieldValue}' for enum field of type '{field.type.type}'."),
                    useErrorMessageContext: false
                  ));
                }
              }

              break;
            }
          }
        }
      }
    }

    static void validateListElementsFields(
      Object containingComponent, IList list,
      bool hasNotNull, bool hasSerializeReference, FieldHierarchy fieldHierarchy,
      IErrorFactory createError, AddError addError, StructureCache structureCache,
      JobController jobController, ImmutableHashSet<string> unityTags,
      Option<CustomObjectValidator> customObjectValidatorOpt,
      Option<UniqueValuesCache> uniqueValuesCache
    ) {
#if DO_PROFILE
      using var _ = new ProfiledScope(nameof(validateListElementsFields));
#endif
      var listItemType = structureCache.getListItemType(list);

      if (listItemType.isUnityObject || hasSerializeReference) {
        if (hasNotNull) {
          jobController.enqueueMainThreadJob(() => {
            int index = 0;
            foreach (var listItem in list) {
              if (listItem == null || listItem.Equals(null)) {
                var hierarchy = fieldHierarchy.push($"[{index}]").asString();
                addError(() => createError.nullField(hierarchy));
              }
              index++;
            }
          });
        }
      }
      if (listItemType.isSerializableAsValue || hasSerializeReference) {
        var index = 0;
        foreach (var listItem in list) {
          validateFields(
            containingComponent, listItem, createError, addError, structureCache, jobController, 
            unityTags, customObjectValidatorOpt, fieldHierarchy.push($"[{index}]"), uniqueValuesCache
          );
          index++;
        }
      }
    }

    static ImmutableList<Object> getSceneObjects(Scene scene) =>
      scene.GetRootGameObjects()
      .Where(go => go.hideFlags == HideFlags.None)
      .Cast<Object>()
      .ToImmutableList();

    static string fullPath(Object o) {
      if (o == null) return "null";
      var go = o as GameObject;
      return
        go && go.transform.parent != null
        ? $"[{fullPath(go.transform.parent.gameObject)}]/{go}"
        : o.ToString();
    }
  }

  [PublicAPI] public static class CustomObjectValidatorExts {
    public static ObjectValidator.CustomObjectValidator join(
      this ObjectValidator.CustomObjectValidator a, ObjectValidator.CustomObjectValidator b
    ) => new JoinedCustomValidator(a, b);
  }

  class JoinedCustomValidator : ObjectValidator.CustomObjectValidator {
    readonly ObjectValidator.CustomObjectValidator a, b;

    public JoinedCustomValidator(ObjectValidator.CustomObjectValidator a, ObjectValidator.CustomObjectValidator b) {
      this.a = a;
      this.b = b;
    }

    public bool isThreadSafe => a.isThreadSafe && b.isThreadSafe;

    public IEnumerable<ErrorMsg> validateField(
      Object containingObject, object obj, ObjectValidator.StructureCache.Field field
    ) => 
      a.validateField(containingObject, obj, field).Concat(b.validateField(containingObject, obj, field));
    
    public IEnumerable<ErrorMsg> validateComponent(Object component) =>
      a.validateComponent(component).Concat(b.validateComponent(component));
  }
}
