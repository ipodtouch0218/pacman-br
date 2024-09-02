#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.CharacterController3DConfig instead")]
public class CharacterController3DConfigAsset : AssetBase {
  public          Quantum.CharacterController3DConfig Settings;
  
  [Obsolete]
  public override Quantum.AssetObject                 AssetObject => Settings;
}

#endif