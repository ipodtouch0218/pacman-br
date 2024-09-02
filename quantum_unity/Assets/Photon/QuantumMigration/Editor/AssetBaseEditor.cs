#if QUANTUM_ENABLE_MIGRATION

namespace Quantum.Editor {
  using System;
  using UnityEditor;
  using System.Linq;
  using UnityEngine;
  
  [Obsolete]
  [CustomEditor(typeof(AssetBase), true)]
  [CanEditMultipleObjects]
  public class QuantumUnityAssetEditor : Editor {
    
    public override void OnInspectorGUI() {
      base.OnInspectorGUI();
      EditorGUILayout.HelpBox("Upgrade legacy Quantum assets with Quantum/Migration menu", MessageType.Warning);
    }
  }
}

#endif