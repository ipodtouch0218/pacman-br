using UnityEngine;
using Quantum;

public unsafe class GhostTargetReticle : QuantumEntityViewComponent {

    //---Serialized Variables
    [SerializeField] private Transform imageTransform;

    //---Private Variables
    private Vector3 target;

    public void Update() {
        imageTransform.position = target;
    }

    public override void OnUpdateView() {
        Frame f = PredictedFrame;
        var ghost = f.Unsafe.GetPointer<Ghost>(EntityRef);
        target = ghost->TargetPosition.ToUnityVector3();
        Update();
    }
}
