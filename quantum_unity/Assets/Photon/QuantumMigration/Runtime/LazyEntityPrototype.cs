#if QUANTUM_ENABLE_MIGRATION

namespace Quantum {
  using System;
  using UnityEditor;
  using UnityEngine;
  using UnityEngine.Serialization;

  public class LazyEntityPrototype : EntityPrototype, ISerializationCallbackReceiver {
    
    [Obsolete]
    protected LazyEntityPrototype() {
      
    }
    
    public QuantumEntityPrototype Parent;
    
    [FormerlySerializedAs("SelfView")]
    public EntityView SelfViewAsset;

    private void OnEnable() {
      if (Parent) {
        Parent.InitializeAssetObject(this, SelfViewAsset);
      }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() {
      // make sure that no components are serialized if the parent is available
      Container = default;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() {
    }
  }
}

#endif