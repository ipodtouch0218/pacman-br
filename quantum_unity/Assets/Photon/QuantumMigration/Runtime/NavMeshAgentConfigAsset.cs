#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.NavMeshAgentConfig instead")]
public class NavMeshAgentConfigAsset : AssetBase {
  public          Quantum.NavMeshAgentConfig Settings;
  
  [Obsolete]
  public override Quantum.AssetObject        AssetObject => Settings;
}

#endif