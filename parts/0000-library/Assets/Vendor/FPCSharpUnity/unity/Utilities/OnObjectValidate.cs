using System.Collections.Generic;
using FPCSharpUnity.core.data;
using FPCSharpUnity.unity.Data;
using UnityEngine;

namespace FPCSharpUnity.unity.Utilities {
  /// <summary>
  /// If a type implements this interface, the Unity object validator will invoke <see cref="onObjectValidate"/> when
  /// validating the object.
  /// </summary>
  public interface OnObjectValidate {
    /// <summary>
    /// Is validation of this object thread safe? If false is returned the validation will be scheduled to be executed
    /// on Unity main thread. 
    /// </summary>
    bool onObjectValidateIsThreadSafe { get; }
    
    /// <summary>
    /// This method is called when `ObjectValidator` begins to validate the object implementing this interface.
    /// </summary>
    /// <param name="containingComponent">
    /// Can be used, for example, to mark any field updates during build time using `.recordEditorChanges`.
    /// </param>
    IEnumerable<ErrorMsg> onObjectValidate(Object containingComponent);
  }
}
