#if QUANTUM_ENABLE_MIGRATION

[System.Obsolete("Use " + nameof(Quantum.Editor.QuantumMapNavMeshUnityEditor) + " instead")]
public static class MapNavMeshEditor {
  public static void UpdateDefaultMinAgentRadius()                => Quantum.Editor.QuantumMapNavMeshUnityEditor.UpdateDefaultMinAgentRadius();
  public static bool BakeUnityNavmesh(UnityEngine.GameObject go)  => Quantum.Editor.QuantumMapNavMeshUnityEditor.BakeUnityNavmesh(go);
  public static bool ClearUnityNavmesh(UnityEngine.GameObject go) => Quantum.Editor.QuantumMapNavMeshUnityEditor.ClearUnityNavmesh(go);
}

#endif