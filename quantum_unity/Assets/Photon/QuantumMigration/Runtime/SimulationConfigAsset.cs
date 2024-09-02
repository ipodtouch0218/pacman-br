#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.SimulationConfig instead")]
public class SimulationConfigAsset : AssetBase {
  public          Quantum.SimulationConfig Settings;
  
  [Obsolete]
  public override Quantum.AssetObject      AssetObject => Settings;
}

#endif