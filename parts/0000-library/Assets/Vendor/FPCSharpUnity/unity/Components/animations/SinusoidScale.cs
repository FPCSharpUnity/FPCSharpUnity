﻿using UnityEngine;

namespace FPCSharpUnity.unity.Components.animations {
  public class SinusoidScale : MonoBehaviour {
    public Vector3 from = Vector3.one, to = Vector3.one;
    public float speed = 1;

    float timeShift;
    bool timeShiftSet;

    internal void Start() {
      if (!timeShiftSet) {
        setTimeShift(Random.value);
      }
    }

    public void OnEnable() {
      Update();
    }

    internal void Update() {
      transform.localScale = Vector3.Lerp(from, to, (Mathf.Sin(Time.time * speed + timeShift) + 1) * .5f);
    }

    public void setTimeShift(float t) {
      timeShiftSet = true;
      timeShift = t * Mathf.PI * 2;
      Update();
    }
  }
}
