using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuantumDebugInput : MonoBehaviour {

    public void OnEnable() {
        QuantumCallback.Subscribe<CallbackPollInput>(this, OnPollInput);
    }
    
    public void OnPollInput(CallbackPollInput callback) {
        Quantum.Input i = new();

        callback.SetInput(i, DeterministicInputFlags.Repeatable);
    }
}
