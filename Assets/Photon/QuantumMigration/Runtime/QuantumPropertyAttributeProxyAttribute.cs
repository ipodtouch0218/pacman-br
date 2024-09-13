namespace Quantum {
  using System;
  using UnityEngine;

  /// <summary>
  /// Obsolete attribute for Quantum 2 compatibility. Non functional in Quantum 3.
  /// </summary>
  [Obsolete]
  [LastSupportedVersion("3.0")]
  public abstract class QuantumPropertyAttributeProxyAttribute : PropertyAttribute {
    /// <summary>
    /// Proxied attribute.
    /// </summary>
    public PropertyAttribute Attribute => this;
  }
}