// <auto-generated>
// This code was auto-generated by a tool, every time
// the tool executes this code will be reset.
//
// If you need to extend the classes generated to add
// fields or methods to them, please create partial
// declarations in another file.
// </auto-generated>
#pragma warning disable 0109
#pragma warning disable 1591


namespace Quantum {
  using UnityEngine;
  
  [UnityEngine.DisallowMultipleComponent()]
  public unsafe partial class QPrototypeGridMover : QuantumUnityComponentPrototype<Quantum.Prototypes.GridMoverPrototype>, IQuantumUnityPrototypeWrapperForComponent<Quantum.GridMover> {
    partial void CreatePrototypeUser(Quantum.QuantumEntityPrototypeConverter converter, ref Quantum.Prototypes.GridMoverPrototype prototype);
    [DrawInline()]
    [ReadOnly(InEditMode = false)]
    public Quantum.Prototypes.GridMoverPrototype Prototype;
    public override System.Type ComponentType {
      get {
        return typeof(Quantum.GridMover);
      }
    }
    public override ComponentPrototype CreatePrototype(Quantum.QuantumEntityPrototypeConverter converter) {
      CreatePrototypeUser(converter, ref Prototype);
      return Prototype;
    }
  }
  [System.ObsoleteAttribute("Use QPrototypeGridMover instead")]
  public abstract unsafe partial class EntityComponentGridMover : QPrototypeGridMover {
  }
}
#pragma warning restore 0109
#pragma warning restore 1591