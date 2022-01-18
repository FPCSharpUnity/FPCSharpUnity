﻿using FPCSharpUnity.core.data;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.functional;

namespace FPCSharpUnity.unity.Data {
  public static class MarkovState {
    public static MarkovState<A> a<A>(
      A data, LazyVal<MarkovTransition<A>[]> transitions
    ) {
      return new MarkovState<A>(data, transitions);
    }
  }

  /** http://en.wikipedia.org/wiki/Markov_chain **/
  public class MarkovState<A> {
    public readonly A data;
    private readonly LazyVal<MarkovTransition<A>[]> transitions;

    public MarkovState(
      A data, LazyVal<MarkovTransition<A>[]> transitions
    ) {
      this.data = data;
      this.transitions = transitions;
    }

    public MarkovState<A> next { get {
      var rng = Rng.now;
      return transitions.strict.randomElementByWeight(_ => _.weight, ref rng).get.nextState;
    } }
  }

  public static class MarkovTransition {
    public static MarkovTransition<A> a<A>(MarkovState<A> nextState, int weight) {
      return new MarkovTransition<A>(nextState, weight);
    }
  }

  public class MarkovTransition<A> {
    public readonly MarkovState<A> nextState;
    public readonly int weight;

    public MarkovTransition(MarkovState<A> nextState, int weight) {
      this.nextState = nextState;
      this.weight = weight;
    }
  }
}
