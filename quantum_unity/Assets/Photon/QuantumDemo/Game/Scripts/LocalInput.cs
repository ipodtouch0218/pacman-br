using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInput : MonoBehaviour {

    private float[] directionInputTimes = new float[4];
    private sbyte targetDirection = -1;

    public void OnEnable() {
        QuantumCallback.Subscribe<CallbackPollInput>(this, PollInput);
    }

    public void Start() {
        for (int i = 0; i < 4; i++) {
            directionInputTimes[i] = -1;
        }
    }

    public void OnMovement(InputValue value) {
        Vector2 input = value.Get<Vector2>();

        for (int i = 0; i < 4; i++) {
            Vector2 target = GridMover.DirectionToVector(i).ToUnityVector2();
            if (Vector2.Dot(input, target) >= 0.5f) {
                // Currently active.
                if (directionInputTimes[i] < 0) {
                    directionInputTimes[i] = Time.time;
                }
            } else {
                directionInputTimes[i] = -1;
            }
        }

        float latestInputTime = 0;
        sbyte latestInput = -1;
        for (int i = 0; i < 4; i++) {
            if (latestInputTime < directionInputTimes[i]) {
                latestInputTime = directionInputTimes[i];
                latestInput = (sbyte) i;
            }
        }

        targetDirection = latestInput;
    }

    public void PollInput(CallbackPollInput callback) {
        Quantum.Input input = new() {
            TargetDirection = targetDirection
        };

        callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }
}
