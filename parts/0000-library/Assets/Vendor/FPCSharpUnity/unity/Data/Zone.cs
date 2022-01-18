﻿using System;
using FPCSharpUnity.unity.Extensions;
using UnityEngine;

namespace FPCSharpUnity.unity.Data {
  [Serializable] public class ZoneSerializable {
    public float width;
    public Vector2 point;
    [Range(0, 360)] public float directionAngle;
    public float directionDistance;

    public Zone toZone() => new Zone(width, point, directionAngle, directionDistance);
  }

  public class Zone {
    public readonly float width;
    public readonly Vector2 point;
    public readonly float directionAngle, directionDistance;
    public readonly Vector2 directionVector;
    public readonly Rect_Vector2 rect;

    public Zone(float width, Vector2 point, float directionAngle, float directionDistance) {
      this.width = width;
      this.point = point;
      this.directionAngle = directionAngle;
      this.directionDistance = directionDistance;
      directionVector = new Vector2(
        directionDistance * Mathf.Cos(directionAngle * Mathf.Deg2Rad),
        directionDistance * Mathf.Sin(directionAngle * Mathf.Deg2Rad)
      );

      var rotated = directionVector.rotate90().normalized * width / 2;
      var ll = point + rotated;
      var lr = point - rotated;
      var ul = ll + directionVector;
      var ur = lr + directionVector;
      rect = new Rect_Vector2(ll, lr, ul, ur);
    }

    /**
     * Given a position returns how much "in the zone" we are in. 0 means out of zone, 1 means at the point.
     *
     * This function ignores movement perpendicular to direction vector.
     **/
    public float percentage(Vector2 position) {
      var direction = point + directionVector;
      var dirProjection = (Vector2) Vector3.Project(position, direction);
      var widthVector = dirProjection - position;

      var widthProjection = (Vector2) Vector3.Project(position, rect.lowerLeft);
      var dirVector = widthProjection - position;

      if (
        widthVector.magnitude < rect.lowerLeft.magnitude &&
        dirVector.magnitude < direction.magnitude
      ) {
        return 1 - position.magnitude / direction.magnitude;
      }
      return 0;
    }
  }
}