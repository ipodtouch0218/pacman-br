#if QUANTUM_ENABLE_MIGRATION

using System;
using Photon.Deterministic;
using Quantum;

[Obsolete("Renamed to " + nameof(QuantumDeterministicSessionConfigAsset))]
public class DeterministicSessionConfigAsset : QuantumDeterministicSessionConfigAsset {
  public new        DeterministicSessionConfig      Config;
  public new static DeterministicSessionConfigAsset Instance => null;
}

#endif