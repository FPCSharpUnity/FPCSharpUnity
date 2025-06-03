
using System;
using FPCSharpUnity.core.macros;
using GenerationAttributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace FPCSharpUnity.unity.Components.sorting_layer {
  /// <summary>
  /// This attempts to solve the problem of objects in the project having their sorting
  /// properties scaterred all over the place.
  ///
  /// To properly sort objects we essentially need to know all the relationships between
  /// all the sorting layers and orders in those layers.
  ///
  /// For example if we have two sorting layers - background and foreground, and an object A
  /// in (background, order 0) and we want object B to be in background, but in from of A,
  /// we need to set its order to some number.
  ///
  /// But what number do we use? 1? 10? 100? There is essentially no way to know unless you
  /// inspect in the runtime all the places where object B can appear and pick a number that makes
  /// sure it is displayed correctly.
  ///
  /// And this problem gets worse with each object that needs its own unique position in a sorting
  /// chain.
  ///
  /// So instead of having these (sorting layer, order in layer) pairs scaterred all over the
  /// project we store them as serialized objects in one directory in a project and have a central
  /// location where we can get an overview of all sorting layers that are used.
  ///
  /// Then we can use components like <see cref="CanvasSortingLayer"/> to set them on actual
  /// objects.
  ///
  /// This makes it much easier to edit existing layers or create new layers somewhere in the
  /// sorting chain.
  ///
  /// ... just another thing Unity should have built-in...
  /// </summary>
  [
    CreateAssetMenu,
    // Help(
    //   HelpType.Info, HelpPosition.Before,
    //   "Sorting layer and order of object in that layer bundled together. " +
    //   "See code for more detailed explanation."
    // )
  ]
  public class SortingLayerReference : ScriptableObject, ISortingLayerReference {
    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
    [SerializeField, SortingLayer] int _sortingLayer;
    [SerializeField] int _orderInLayer;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

    #endregion

    public int sortingLayer => _sortingLayer;
    public int orderInLayer => _orderInLayer;
    
    public RuntimeSortingLayerReference withOffset(int offset) {
      return new RuntimeSortingLayerReference(_sortingLayer, _orderInLayer + offset);
    }
  }
  
  [Serializable]
  public class SerializableSortingLayerReference : ISortingLayerReference {
    #region Unity Serialized Fields

#pragma warning disable 649
    // ReSharper disable NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
    [SerializeField, SortingLayer] int _sortingLayer;
    [SerializeField] int _orderInLayer;
    // ReSharper restore NotNullMemberIsNotInitialized, FieldCanBeMadeReadOnly.Local, ConvertToConstant.Local
#pragma warning restore 649

    #endregion

    public int sortingLayer => _sortingLayer;
    public int orderInLayer => _orderInLayer;
  }
  
  public class RuntimeSortingLayerReference : ISortingLayerReference {
    public int sortingLayer { get; }
    public int orderInLayer { get; }
    
    public RuntimeSortingLayerReference(int sortingLayer, int orderInLayer) {
      this.sortingLayer = sortingLayer;
      this.orderInLayer = orderInLayer;
    }
  }

  public interface ISortingLayerReference {
    public int sortingLayer { get; }
    public int orderInLayer { get; }
  }

  [Singleton] public partial class SortingLayerReferenceDefault : ISortingLayerReference {
    public int sortingLayer => 0;
    public int orderInLayer => 0;
  }

  public static class SortingLayerExts {
    public static void applyTo(this ISortingLayerReference layerRef, Canvas canvas) {
      canvas.sortingLayerID = layerRef.sortingLayer;
      canvas.sortingOrder = layerRef.orderInLayer;
    }

    public static void applyTo(this ISortingLayerReference layerRef, Renderer renderer) {
      renderer.sortingLayerID = layerRef.sortingLayer;
      renderer.sortingOrder = layerRef.orderInLayer;
    }

    public static void applyTo(this ISortingLayerReference layerRef, SortingGroup soringGroup) {
      soringGroup.sortingLayerID = layerRef.sortingLayer;
      soringGroup.sortingOrder = layerRef.orderInLayer;
    }

    public static void applyTo(this ISortingLayerReference layerRef, ParticleSystem[] particleSystems) {
      foreach (var system in particleSystems) {
        applyTo(layerRef, system.GetComponent<Renderer>());
      }
    }
  }
}
