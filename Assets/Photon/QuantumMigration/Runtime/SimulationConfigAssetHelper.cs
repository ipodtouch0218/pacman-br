#if QUANTUM_ENABLE_MIGRATION

using System;
using Quantum;

public static class SimulationConfigAssetHelper {
  [Obsolete("Use Quantum.SimulationConfig.PhysicsType instead")]
  public enum PhysicsType {
    Physics3D = SimulationConfig.PhysicsType.Physics3D,
    Physics2D = SimulationConfig.PhysicsType.Physics2D
  }

  [Obsolete("Use Quantum.SimulationConfig.ImportLayersFromUnity() instead")]
  public static void ImportLayersFromUnity(this SimulationConfig data, PhysicsType physicsType = PhysicsType.Physics3D) => data.ImportLayersFromUnity((SimulationConfig.PhysicsType)physicsType);

  [Obsolete("Use Quantum.SimulationConfig.GetUnityLayerNameArray() instead")]
  public static String[] GetUnityLayerNameArray() => SimulationConfig.GetUnityLayerNameArray();

  [Obsolete("Use Quantum.SimulationConfig.GetUnityLayerMatrix() instead")]
  public static Int32[] GetUnityLayerMatrix(PhysicsType physicsType) => SimulationConfig.GetUnityLayerMatrix((SimulationConfig.PhysicsType)physicsType);
}

#endif