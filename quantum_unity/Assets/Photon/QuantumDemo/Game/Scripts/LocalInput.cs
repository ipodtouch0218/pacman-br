using Photon.Deterministic;
using Quantum;
using UnityEngine;

public class LocalInput : MonoBehaviour {

    private void OnEnable() {
        QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    public void PollInput(CallbackPollInput callback) {
        Quantum.Input input = new Quantum.Input();

        float x = UnityEngine.Input.GetAxis("Horizontal");
        float y = UnityEngine.Input.GetAxis("Vertical");

        input.Up = y > 0.5;
        input.Right = x > 0.5;
        input.Down = y < -0.5;
        input.Left = x < -0.5;

        callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }
}
