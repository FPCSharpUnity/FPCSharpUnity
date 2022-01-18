#if UNITY_ANDROID
using FPCSharpUnity.core.functional;
using FPCSharpUnity.core.exts;
using FPCSharpUnity.core.test_framework;
using NUnit.Framework;

namespace FPCSharpUnity.unity.Android.Bindings.com.google.firebase.analytics {
  public class FirebaseEventParamTest {
    [Test]
    public void ItShouldConvertStrings() =>
      "root.TLPAdsInit.ads.provider setup.MobFox".firebaseParam(FirebaseEvent.Trim.None)
      .shouldEqual(new OneOf<string, long, double>("root.TLPAdsInit.ads.provider setup.MobFox"));

    [Test]
    public void ItShouldLeftTrimStrings() =>
      "root.TLPAdsInit.ads.provider setup.MobFox".firebaseParam(FirebaseEvent.Trim.KeepLeftSide)
      .shouldEqual(new OneOf<string, long, double>("root.TLPAdsInit.ads.provider setup.M"));

    [Test]
    public void ItShouldRightTrimStrings() =>
      "root.TLPAdsInit.ads.provider setup.MobFox".firebaseParam(FirebaseEvent.Trim.KeepRightSide)
      .shouldEqual(new OneOf<string, long, double>("TLPAdsInit.ads.provider setup.MobFox"));
  }

  public class FirebaseEventTest {
    [Test]
    public void ItShouldCreateValidParams() {
      var paramValue = "v".repeat(FirebaseEvent.MAX_PARAM_VALUE_LENGTH).firebaseParam();
      var parameters = FirebaseEvent.createEmptyParams();
      for (var idx = 0; idx < FirebaseEvent.MAX_PARAM_COUNT; idx++) {
        var key = idx.ToString().PadLeft(FirebaseEvent.MAX_PARAM_KEY_LENGTH, 'a');
        parameters.Add(key, paramValue);
      }
      FirebaseEvent.a(
        "e".repeat(FirebaseEvent.MAX_EVENT_LENGTH),
        parameters
      ).shouldBeRight();
    }

    [Test]
    public void ItShouldFailIfEventNameIsReserved() => Assert.Ignore();

    [Test]
    public void ItShouldFailIfEventNameIsInvalid() => Assert.Ignore();

    [Test]
    public void ItShouldFailIfItHasTooManyParameters() => Assert.Ignore();

    [Test]
    public void ItShouldFailIfParameterKeyIsInvalid() => Assert.Ignore();

    [Test]
    public void ItShouldFailIfParameterValueIsTooLong() => Assert.Ignore();
  }
}
#endif