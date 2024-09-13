#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.Map instead")]
public class MapAsset : AssetBase {
  public          Quantum.Map         Settings;
  
  [Obsolete]
  public override Quantum.AssetObject AssetObject => Settings;
}

#endif