#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.NavMesh instead")]
public class NavMeshAsset : AssetBase {
  public          Quantum.NavMesh     Settings;
  
  [Obsolete]
  public override Quantum.AssetObject AssetObject => Settings;
}

#endif