using Quantum;
using UnityEngine;
using UnityEngine.Events;

public class TeleportingEntityView : EntityView {

    //---Serialized Variables
    [SerializeField] private UnityEvent<bool> onTeleportStateChanged;

    //---Private Variables
    private bool isTeleporting;

    public void Start() {
        QuantumEvent.Subscribe<EventTeleportEvent>(this, OnTeleportEvent);
    }

    public void OnTeleportEvent(EventTeleportEvent e) {
        if (e.Entity != EntityRef) {
            return;
        }

        isTeleporting = e.IsTeleporting;
        onTeleportStateChanged?.Invoke(isTeleporting);
    }

    protected override void ApplyTransform(ref UpdatePostionParameter param) {
        if (Vector3.Distance(param.NewPosition, param.UninterpolatedPosition) > 10) {
            isTeleporting = true;
        }
        if (isTeleporting) {
            transform.SetPositionAndRotation(param.UninterpolatedPosition, param.UninterpolatedRotation);
        } else {
            base.ApplyTransform(ref param);
        }

    }
}