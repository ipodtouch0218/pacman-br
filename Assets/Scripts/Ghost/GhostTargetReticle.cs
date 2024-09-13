using UnityEngine;
using Quantum;

public class GhostTargetReticle : QuantumCallbacks {

    //---Serialized Variables
    [SerializeField] private Transform imageTransform;
    [SerializeField] private EntityView entity;

    //---Private Variables
    private Vector3 target;

    public void Update() {
        imageTransform.position = target;
    }

    public override void OnUpdateView(QuantumGame game) {
        var frame = game.Frames.Predicted;
        var ghost = frame.Get<Ghost>(entity.EntityRef);

        target = ghost.TargetPosition.ToUnityVector3();
        Update();
    }
}
