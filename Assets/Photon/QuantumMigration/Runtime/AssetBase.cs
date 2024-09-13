#if QUANTUM_ENABLE_MIGRATION

using System;
using UnityEngine;

[Obsolete("Use AssetObject instead")]
public abstract partial class AssetBase : Quantum.Migration.LegacyAssetObjectWrapper {

  public const string DefaultAssetObjectPropertyPath = "Settings";
  public const char NestedPathSeparator = '|';


  
  public bool IsTransient => false;



  public virtual void Loaded() {
    PrepareAsset();
  }

  public virtual void PrepareAsset() {
  }

  public virtual void Disposed() {
  }

  public virtual void Reset() {
  }

  public virtual void Awake() {
  }
  
#if UNITY_EDITOR
  public virtual void OnInspectorGUIBefore(UnityEditor.SerializedObject serializedObject) {
        
  }
  
  public virtual void OnInspectorGUIAfter(UnityEditor.SerializedObject serializedObject) {
        
  }
#endif
}

#endif