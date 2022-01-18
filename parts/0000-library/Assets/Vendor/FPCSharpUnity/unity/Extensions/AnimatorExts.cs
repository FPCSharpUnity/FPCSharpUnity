using System.Collections.Generic;
using System.Linq;
using FPCSharpUnity.unity.Data;
using FPCSharpUnity.core.collection;
using FPCSharpUnity.core.exts;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  public static class AnimatorExts {
    /// <summary>
    /// When accessing animator parameters from editor, the animator can be not loaded into memory and the parameters
    /// will just return an empty array.
    ///
    /// This behavior is explained in https://forum.unity.com/threads/animator-parameters-array-empty-when-in-prefab.335134/
    ///
    /// This tries to get the parameters out of the editor animator controller when running in Unity Editor.
    /// </summary>
    public static AnimatorControllerParameter[] parametersInEditor(this Animator animator) {
#if UNITY_EDITOR
      return animator.runtimeAnimatorController is UnityEditor.Animations.AnimatorController ctrl
        ? ctrl.parameters
        : animator.parameters;
#else
      return animator.parameters;
#endif
    }
    
    public static string[] getParameters(this Animator animator, AnimatorControllerParameterType type) => animator
      ? animator.parametersInEditor().Where(_ => _.type == type).Select(_ => _.name)
        .ToArray()
      : EmptyArray<string>._;
  }
}