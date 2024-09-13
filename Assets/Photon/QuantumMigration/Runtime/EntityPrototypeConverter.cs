#if QUANTUM_ENABLE_MIGRATION

using System;
using Quantum;

[Obsolete("Use " + nameof(QuantumEntityPrototypeConverter) + " instead.")]
public sealed unsafe partial class EntityPrototypeConverter : QuantumEntityPrototypeConverter {
  public EntityPrototypeConverter(QuantumMapData map, QuantumEntityPrototype[] orderedMapPrototypes) : base(map, orderedMapPrototypes) {
  }
  public EntityPrototypeConverter(QuantumEntityPrototype prototypeAsset) : base(prototypeAsset) {
  }
}

#endif