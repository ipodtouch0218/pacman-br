#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.PhysicsMaterial instead")]
public class PhysicsMaterialAsset : AssetBase {
  public          Quantum.PhysicsMaterial Settings;
  
  [Obsolete]
  public override Quantum.AssetObject     AssetObject => Settings;
}

#endif