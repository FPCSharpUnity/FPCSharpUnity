﻿using FPCSharpUnity.unity.Data;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Extensions {
  [PublicAPI]
  public static class ComponentExts {
    public static A clone<A>(
      this A self, Vector3? position=null, Quaternion? rotation=null, Transform parent=null,
      int? siblingIndex=null, bool? setActive=null
    ) where A : Component {

      // Setting parent through instantiate is faster than first creating object and then setting it's parent.
      // https://youtu.be/n-oZa4Fb12U?t=1386
      var cloned = parent != null ? Object.Instantiate(self, parent, false) : Object.Instantiate(self);
      if (position != null) cloned.transform.position = (Vector3) position;
      if (rotation != null) cloned.transform.rotation = (Quaternion) rotation;
      if (siblingIndex != null) cloned.transform.SetSiblingIndex((int) siblingIndex);
      if (setActive != null) cloned.gameObject.SetActive((bool) setActive);
      return cloned;
    }

    public static A clone<A, Data>(
      this A self, Data data, Vector3? position = null, Quaternion? rotation = null, Transform parent = null,
      int? siblingIndex = null, bool? setActive = null
    ) where A : Component, ISetupableComponent<Data> {
      var a = self.clone(position, rotation, parent, siblingIndex, setActive);
      a.setup(data);
      return a;
    }

    public static Option<A> GetComponentOption<A>(this Component c) where A : Object => 
      c.TryGetComponent<A>(out var component) ? Some.a(component) : None._;

    public static Option<A> GetComponentInChildrenOption<A>(this GameObject o) where A : Object =>
      F.opt(o.GetComponentInChildren<A>());

    public static Option<A> GetComponentInChildrenOption<A>(this Component c) where A : Object =>
      F.opt(c.GetComponentInChildren<A>());

    public static void destroyGameObject(this Component c) => c.gameObject.destroySafe();
    public static void destroyComponent(this Component c) => c.destroySafe();

    /// <summary>
    /// This is made so that you could do:
    ///
    /// <code><![CDATA[
    /// if (c.setActiveGO(boolean)) {
    ///   // do stuff with c
    /// }
    /// ]]></code>
    /// </summary>
    /// <returns>the value you passed</returns>
    public static bool setActiveGO(this Component c, bool active) {
      c.gameObject.SetActive(active);
      return active;
    }
  }
}
