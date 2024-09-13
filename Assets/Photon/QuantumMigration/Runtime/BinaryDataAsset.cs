#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.BinaryData instead")]
public class BinaryDataAsset : AssetBase {
  public          Quantum.BinaryData  Settings;
  
  [Obsolete]
  public override Quantum.AssetObject AssetObject => Settings;
}

#endif