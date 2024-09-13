namespace Quantum {
  using System;
  using UnityEngine;

  /// <summary>
  /// Obsolete attribute for Quantum 2 compatibility. Non functional in Quantum 3.
  /// </summary>
  [AttributeUsage(AttributeTargets.Field)]
  [Obsolete]
  [LastSupportedVersion("3.0")]
  public class QuantumInspectorAttribute : PropertyAttribute {
  }
}