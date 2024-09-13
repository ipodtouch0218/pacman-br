#if QUANTUM_ENABLE_MIGRATION

[System.Obsolete("Use " + nameof(Quantum.QuantumEntityViewBindBehaviour) + " instead")]
public enum EntityViewBindBehaviour {
  NonVerified = Quantum.QuantumEntityViewBindBehaviour.NonVerified,
  Verified    = Quantum.QuantumEntityViewBindBehaviour.Verified
}

#endif