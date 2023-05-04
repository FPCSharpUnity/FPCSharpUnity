﻿using FPCSharpUnity.core.exts;
using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using FPCSharpUnity.core.functional;
using UnityEditor;
using UnityEngine;

namespace FPCSharpUnity.unity.Components.EditorTools {
  [CustomEditor(typeof(ObjectCloner))]
  public class ObjectClonerEditor : UnityEditor.Editor {
    enum LockedAxis2 { A, B }

    Option<GameObject> objectToMoveAroundOpt;
    Option<LockedAxis2> lockedAxis2 = None._;
    Option<Vector3> lastPlacedPosition = None._;

    static Option<LockedAxis2> next(Option<LockedAxis2> current) =>
        current.isNone ? Some.a(LockedAxis2.A)
      : current.exists(LockedAxis2.A) ? Some.a(LockedAxis2.B)
      : None._;

    public override void OnInspectorGUI() {
      const string msg =
        "Helps to clone objects easily.\n" +
        "\n" +
        "Set the fields, hold CTRL, " +
        "then move your mouse around in scene, click to place.\n" +
        "\n" +
        "Right click to change secondary locked axis. Secondary locking " +
        "locks from last placed object position.";

      EditorGUILayout.HelpBox(msg, MessageType.Info);
      base.OnInspectorGUI();
    }

    [UsedImplicitly]
    void OnSceneGUI() {
      var _target = (ObjectCloner) target;
      var currentEvent = Event.current;

      if (currentEvent.control) {
        // Consume mouse clicks so we wouldn't deselect the object.
        if (currentEvent.type == EventType.Layout) HandleUtility.AddDefaultControl(0);

        foreach (var data in _target.editorData) {
          var newObjPositionOpt = newObjPosition(
            currentEvent.mousePosition,
            lastPlacedPosition.getOrElse(data.sourceTransform.position),
            _target.lockedAxis, lockedAxis2
          );

          if (objectToMoveAroundOpt.isNone) {
            foreach (var position in newObjPositionOpt) {
              var instantiator = new PrefabInstantiator(data.prefab);
              var obj = instantiator.instantiate();
              obj.transform.position = position;
              obj.transform.parent = _target.transform;
              obj.transform.rotation = data.sourceTransform.rotation;
              obj.transform.localScale = data.sourceTransform.localScale;
              objectToMoveAroundOpt = Some.a(obj);
            }
          }

          foreach (var obj in objectToMoveAroundOpt) {
            if (currentEvent.type == EventType.MouseMove) {
              foreach (var position in newObjPositionOpt) {
                obj.transform.position = position;
              }
            }

            if (currentEvent.type == EventType.MouseUp) {
              switch (currentEvent.button) {
                case 0:
                  Undo.RegisterCreatedObjectUndo(obj, $"Object ({obj.name}) created");
                  obj.transform.parent = _target.parent.getOrNull();
                  lastPlacedPosition = Some.a(obj.transform.position);
                  objectToMoveAroundOpt = None._;
                  break;
                case 1:
                  lockedAxis2 = next(lockedAxis2);
                  break;
              }
            }
          }
        }
      }
      else {
        foreach (var obj in objectToMoveAroundOpt) DestroyImmediate(obj);
        objectToMoveAroundOpt = None._;
        lockedAxis2 = None._;
        lastPlacedPosition = None._;
      }
    }

    // Determines where on screen we are currently pointing, given that we have one
    // axis locked.
    static Option<Vector3> newObjPosition(
      Vector2 mousePosition, Vector3 originPosition,
      ObjectCloner.LockedAxis lockedAxis, Option<LockedAxis2> secondaryLockedAxis
    ) {
      var ray = posInWorld(mousePosition);
      var shiftedPosition = new Vector3(
        lockedAxis == ObjectCloner.LockedAxis.X ? originPosition.x : originPosition.x - 1,
        lockedAxis == ObjectCloner.LockedAxis.Y ? originPosition.y : originPosition.y - 1,
        lockedAxis == ObjectCloner.LockedAxis.Z ? originPosition.z : originPosition.z - 1
      );

      return secondaryLockedAxis.isSome
        ? projectToLine(ray, originPosition, shiftedPosition, lockedAxis, secondaryLockedAxis.get)
        : projectToPlane(ray, originPosition, shiftedPosition);
    }

    static Option<Vector3> projectToPlane(
      Ray ray, Vector3 originPosition, Vector3 shiftedPosition
    ) {
      var point1 = new Vector3(shiftedPosition.x, shiftedPosition.y, originPosition.z);
      var point2 = new Vector3(shiftedPosition.x, originPosition.y, shiftedPosition.z);
      var point3 = new Vector3(originPosition.x, shiftedPosition.y, shiftedPosition.z);

      var plane = new Plane(point1, point2, point3);

      float distance;
      plane.Raycast(ray, out distance);

      return distance > 0
        ? Some.a(ray.GetPoint(distance))
        : None._;
    }

    static Option<Vector3> projectToLine(
      Ray ray, Vector3 originPosition, Vector3 shiftedPosition,
      ObjectCloner.LockedAxis lockedAxis, LockedAxis2 secondaryLockedAxis
    ) => projectToPlane(ray, originPosition, shiftedPosition).map(inPlane => {
      var lineP1 = originPosition;
      var lineP2 =
        lockedAxis == ObjectCloner.LockedAxis.X ? new Vector3(
          originPosition.x,
          (secondaryLockedAxis == LockedAxis2.B ? originPosition : shiftedPosition).y,
          (secondaryLockedAxis == LockedAxis2.A ? originPosition : shiftedPosition).z
        )
        : lockedAxis == ObjectCloner.LockedAxis.Y ? new Vector3(
          (secondaryLockedAxis == LockedAxis2.A ? originPosition : shiftedPosition).x,
          originPosition.y,
          (secondaryLockedAxis == LockedAxis2.B ? originPosition : shiftedPosition).z
        )
        : new Vector3(
          (secondaryLockedAxis == LockedAxis2.B ? originPosition : shiftedPosition).x,
          (secondaryLockedAxis == LockedAxis2.A ? originPosition : shiftedPosition).y,
          originPosition.z
        );

      var direction = (lineP2 - lineP1) * 10000;
      Handles.DrawLine(originPosition - direction, originPosition + direction);

      var projected = Vector3.Project(inPlane - originPosition, direction) + originPosition;
      return projected;
    });

    static Ray posInWorld(Vector3 mousePosition) => HandleUtility.GUIPointToWorldRay(mousePosition);
  }
}
