using FPCSharpUnity.unity.Functional;
using JetBrains.Annotations;
using Plugins.FPCSharpUnity.Components;
using FPCSharpUnity.core.functional;
using UnityEngine;

namespace FPCSharpUnity.unity.Components {
  [
    RequireComponent(typeof(MeshFilter)),
    RequireComponent(typeof(MeshRenderer)),
    ExecuteInEditMode
  ]
  public class TrailDrawer : TrailDrawerBase {

    #region Unity Serialized Fields

#pragma warning disable 649
// ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
    [SerializeField] float trailWidth = 3;
    [SerializeField, NotNull] Gradient color = new Gradient();
    [SerializeField, NotNull] AnimationCurve widthMultiplierCurve;
// ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local
#pragma warning restore 649

    #endregion

    readonly LineMeshGenerator.GetNode getNode;
    readonly LazyVal<LineMeshGenerator> lineMeshGenerator;

    public TrailDrawer() {
      getNode = idx => new LineMeshGenerator.NodeData(
        relativePosition: nodes[idx].position - getTransformPosition(),
        distanceToPrevNode: nodes[idx].distanceToPrevNode
      );
      lineMeshGenerator = Lazy.a(() => new LineMeshGenerator(
        trailWidth, gameObject.GetComponent<MeshFilter>(), color, widthMultiplierCurve)
      );
    }

    public override void LateUpdate() {
      base.LateUpdate();
      lineMeshGenerator.strict.update(
        totalPositions: nodes.Count,
        totalLineLength: calculateTotalLength(),
        getNode: getNode
      );
      // Trail should not be rotated with the parent
      transform.rotation = Quaternion.identity;
    }

    float calculateTotalLength() {
      var sum = 0f;
      for (var i = 0; i < nodes.Count; i++) {
        sum += nodes[i].distanceToPrevNode;
      }
      return sum;
    }
  }
}
