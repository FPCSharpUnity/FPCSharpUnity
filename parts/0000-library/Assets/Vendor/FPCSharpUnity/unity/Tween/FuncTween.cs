﻿using System;

namespace FPCSharpUnity.unity.Tween {
  /** TweenFacade helper class to make tweens type-safe **/
  public static class TF {
    /** Name of the property you need to pass to go tween **/
    public const string Prop = "value";

    public static TweenFacade<A> a<A>(Func<A> getter, Action<A> setter) {
      return new TweenFacade<A>(getter, setter);
    }
  }

  public
#if ENABLE_IL2CPP
    class
#else
    struct
#endif
    TweenFacade<A> {
    private readonly Func<A> getter;
    private readonly Action<A> setter;

    public TweenFacade(Func<A> getter, Action<A> setter) {
      this.getter = getter;
      this.setter = setter;
    }

    public A value {
      get { return getter(); }
      set { setter(value); }
    }
  }

#if GOTWEEN
  public static class TweenCfg {
    public static TweenCfg<A> a<A>(TweenFacade<A> facade, GoTweenConfig config) {
      return new TweenCfg<A>(facade, config);
    }
  }

  public struct TweenCfg<A> {
    public readonly TweenFacade<A> facade;
    public readonly GoTweenConfig config;

    public TweenCfg(TweenFacade<A> facade, GoTweenConfig config) {
      this.facade = facade;
      this.config = config;
    }
  }
#endif
}
