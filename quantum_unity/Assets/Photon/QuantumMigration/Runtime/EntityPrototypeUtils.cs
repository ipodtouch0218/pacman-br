#if QUANTUM_ENABLE_MIGRATION

using System;
using Quantum;
using UnityEngine;

public static class EntityPrototypeUtils {
  [Obsolete("Use " + nameof(QPrototypePhysicsCollider2D) + "." + nameof(QPrototypePhysicsCollider2D.TrySetShapeConfigFromSourceCollider) + " instead")]
  public static bool TrySetShapeConfigFromSourceCollider2D(Shape2DConfig config, Transform reference, Component collider) {
    return QPrototypePhysicsCollider2D.TrySetShapeConfigFromSourceCollider(config, reference, collider, out _);
  }

  [Obsolete("Use " + nameof(QPrototypePhysicsCollider3D) + "." + nameof(QPrototypePhysicsCollider3D.TrySetShapeConfigFromSourceCollider) + " instead")]
  public static bool TrySetShapeConfigFromSourceCollider3D(Shape3DConfig config, Transform reference, Component collider) {
    return QPrototypePhysicsCollider3D.TrySetShapeConfigFromSourceCollider(config, reference, collider, out _);
  }

  [Obsolete]
  public static bool IsColliderTrigger(this Component component) {
#if QUANTUM_ENABLE_PHYSICS2D && !QUANTUM_DISABLE_PHYSICS2D
      if (component is Collider2D collider2D) {
        return collider2D.isTrigger;
      }
#endif
#if QUANTUM_ENABLE_PHYSICS3D && !QUANTUM_DISABLE_PHYSICS3D
      if (component is Collider collider3D) {
        return collider3D.isTrigger;
      }
#endif
    return false;
  }
}

#endif