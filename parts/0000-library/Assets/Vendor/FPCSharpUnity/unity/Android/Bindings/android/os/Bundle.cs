﻿#if UNITY_ANDROID
using GenerationAttributes;
using UnityEngine;

namespace FPCSharpUnity.unity.Android.Bindings.android.os {
  [JavaBinding(JAVA_CLASS)]
  public class Bundle : BaseBundle {
    const string JAVA_CLASS = "android.os.Bundle";
    public Bundle() : base(new AndroidJavaObject(JAVA_CLASS)) {}
    public Bundle(AndroidJavaObject java) : base(java) {}

    public void putByte(string key, byte value) => java.Call("putByte", key, value);
    public void putByteArray(string key, byte[] value) => java.Call("putByteArray", key, value);
    public void putChar(string key, char value) => java.Call("putChar", key, value);
    public void putCharArray(string key, char[] value) => java.Call("putCharArray", key, value);
    public void putFloat(string key, float value) => java.Call("putFloat", key, value);
    public void putFloatArray(string key, float[] value) => java.Call("putFloatArray", key, value);
    public void putShort(string key, short value) => java.Call("putShort", key, value);
    public void putShortArray(string key, short[] value) => java.Call("putShortArray", key, value);
  }
}
#endif