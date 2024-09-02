#if QUANTUM_ENABLE_MIGRATION

using UnityEngine;
using System;

[Obsolete("Use Quantum.EntityPrototype instead")]
public class EntityPrototypeAsset : AssetBase {
  public          Quantum.EntityPrototype Settings;
  [Obsolete]
  public override Quantum.AssetObject     AssetObject => Settings;
}

#endif