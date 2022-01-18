using System;

namespace FPCSharpUnity.unity.Android.Ads {
  public interface IStandardRewarded : IStandardInterstitial {
    event Action<bool> adWatched;
  }
}