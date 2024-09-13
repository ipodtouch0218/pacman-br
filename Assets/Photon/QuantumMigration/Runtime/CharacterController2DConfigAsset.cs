#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.CharacterController2DConfig instead")]
public class CharacterController2DConfigAsset : AssetBase {
  public          Quantum.CharacterController2DConfig Settings;
  
  [Obsolete]
  public override Quantum.AssetObject                 AssetObject => Settings;
}

#endif