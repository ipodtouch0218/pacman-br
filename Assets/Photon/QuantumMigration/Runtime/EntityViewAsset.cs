#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.EntityView instead")]
public class EntityViewAsset : AssetBase {
  public          Quantum.EntityView  Settings;
  
  [Obsolete]
  public override Quantum.AssetObject AssetObject => Settings;
}

#endif