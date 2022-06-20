using System;
using System.Linq;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Components.Interfaces;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Tween.fun_tween.serialization.manager;
using FPCSharpUnity.unity.Tween.fun_tween.serialization.tweeners;
using FPCSharpUnity.unity.unity_serialization;
using FPCSharpUnity.unity.validations;
using GenerationAttributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FPCSharpUnity.unity.Components {
  /// <summary>
  /// Allows us to apply <see cref="Formula"/> using multiple <see cref="_variables"/> and set the result using
  /// <see cref="_setters"/>. This is very useful when we want to change a field on same <see cref="MonoBehaviour"/> from
  /// multiple <see cref="FunTweenManagerV2"/>s.
  /// </summary>
  [ExecuteAlways]
  public partial class PercentageCombinerSetter : MonoBehaviour, IMB_Start {
#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, UseArrayEmptyMethod
    [SerializeField, NotNull, PublicAccessor, InlineProperty, HideLabel, Title("Variables"), InfoBox(
       "Variables that get combined into a single value based on 'Formula' field\n\n" +
       "Key - id of the variable that is used to reference it\n" +
       "Value - current value of the variable"
     )] SerializableDictionaryMutable<string, Percentage> _variables;
    [SerializeField] Formula _formula;
    [SerializeReference, NotNull, InfoBox(
       "Every time any of the variables changes, new value gets calculated and applied to all setters."
     )] ISetter[] _setters = new ISetter[0];
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, UseArrayEmptyMethod
#pragma warning restore 649

    public void set(string id, Percentage value) {
      _variables.set(id, value);
      update();
    }

    public enum Formula {
      /// <summary> Finds maximum of all <see cref="_variables"/>. </summary>
      Max = 0, 
      /// <summary> Finds minimum of all <see cref="_variables"/>. </summary>
      Min = 1
    }

    void update() {
      var value = calculate();

      foreach (var setter in _setters) {
        setter.value = value;
      }
    }

    public Percentage calculate() {
      var value = _formula switch {
        Formula.Max => 0f,
        Formula.Min => 1f,
        _ => throw _formula.argumentOutOfRange()
      };
    
      foreach (var kvp in _variables.a) {
        value = _formula switch {
          Formula.Max => Mathf.Max(kvp.Value.value, value),
          Formula.Min => Mathf.Min(kvp.Value.value, value),
          _ => throw _formula.argumentOutOfRange()
        };
      }
      return new Percentage(value);
    }

    public void Start() => update();
  
    public interface ISetter {
      Percentage value { set; get; }
    }
  
    [Serializable] public class CanvasGroupAlphaSetter : ISetter {
#pragma warning disable 649
      // ReSharper disable NotNullMemberIsNotInitialized
      [SerializeField, NotNull] CanvasGroup _canvasGroup;
      // ReSharper restore NotNullMemberIsNotInitialized
#pragma warning restore 649

      public Percentage value {
        get => new Percentage(_canvasGroup.alpha);
        set => _canvasGroup.alpha = value.value;
      }
    }
  }
}