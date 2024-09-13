#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.PolygonCollider instead")]
public class PolygonColliderAsset : AssetBase {
  public          Quantum.PolygonCollider Settings;
  
  [Obsolete]
  public override Quantum.AssetObject     AssetObject => Settings;
}

#endif