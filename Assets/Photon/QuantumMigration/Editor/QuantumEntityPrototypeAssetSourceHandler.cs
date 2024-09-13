#if QUANTUM_ENABLE_MIGRATION
namespace Quantum.Editor {
  using System;
  using UnityEditor;
  using UnityEditor.AssetImporters;
  using UnityEngine;

  class QuantumEntityPrototypeAssetSourceHandler : QuantumMonoBehaviourAssetSourceHandler {
    public override Type   MonoBehaviourType => typeof(QuantumEntityPrototype);
    public override Type   AssetType         => typeof(Quantum.LazyEntityPrototype);
    public override int    Order             => 2000;
    public override string NestedNameSuffix  => "EntityPrototype";

    public override void OnPostprocessPrefab(AssetImportContext context, MonoBehaviour monoBehaviour) {
      // nothing to do here
    }

    public override bool OnValidateQuantumAsset(GameObject prefab, string prefabPath, AssetObject asset) {
      var lazyPrototype = asset as LazyEntityPrototype;
      if (!lazyPrototype) {
        return false;
      }

      QuantumEntityPrototype parent = prefab.GetComponent<QuantumEntityPrototype>();
      Quantum.EntityView selfView = AssetDatabase.LoadAssetAtPath<Quantum.EntityView>(prefabPath);
      
      if (parent != lazyPrototype.Parent || selfView != lazyPrototype.SelfViewAsset) {
        lazyPrototype.Parent = parent;
        lazyPrototype.SelfViewAsset = selfView;
        return true;
      }
      
      return false;
    }
  }
}
#endif