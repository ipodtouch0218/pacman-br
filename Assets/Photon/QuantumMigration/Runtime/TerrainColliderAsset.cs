#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.TerrainCollider instead")]
public class TerrainColliderAsset : AssetBase {
  public Quantum.TerrainCollider Settings;

  [Obsolete]
  public override Quantum.AssetObject AssetObject => Settings;
}

#endif