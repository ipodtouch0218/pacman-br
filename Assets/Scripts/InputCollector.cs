using Quantum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputCollector : MonoBehaviour {

    //---Serailized Variables
    [SerializeField] private List<InputActionReference> movementActions;
    [SerializeField] private InputActionReference bombAction;

    //---Private Variables
    private List<sbyte> heldDirections = new(4);
    private sbyte lastHeldDirection = -1;

    public void Start() {
        QuantumCallback.Subscribe<CallbackPollInput>(this, OnPollInput);

        foreach (var action in movementActions) {
            action.action.actionMap.Enable();
            action.action.performed += Performed;
            action.action.canceled += Performed;
        }
    }

    public void OnDestroy() {
        foreach (var action in movementActions) {
            action.action.performed -= Performed;
            action.action.canceled -= Performed;
        }
    }

    private void Performed(InputAction.CallbackContext context) {
        sbyte direction = (sbyte) movementActions.FindIndex(iar => iar.action == context.action);

        if (direction != -1) {
            if (context.performed) {
                heldDirections.Add(direction);
            } else if (context.canceled) {
                heldDirections.Remove(direction);
            }
        }
    }

    public void OnPollInput(CallbackPollInput callback) {
        sbyte target = heldDirections.Count > 0 ? heldDirections[^1] : lastHeldDirection;
        lastHeldDirection = target;

        Quantum.Input input = new() {
            TargetDirection = target,
            Bomb = bombAction.action.ReadValue<float>() > 0.5f
        };

        callback.SetInput(input, Photon.Deterministic.DeterministicInputFlags.Repeatable);
    }
}