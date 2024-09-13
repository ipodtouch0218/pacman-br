using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputCollector : MonoBehaviour {

    //---Serailized Variables
    [SerializeField] private InputActionReference movementAction, bombAction;

    //---Private Variables
    private float[] directionTimes;

    public void Start() {
        //reservePowerupAction.action.performed += OnPowerupAction;
        QuantumCallback.Subscribe<CallbackPollInput>(this, OnPollInput);
        directionTimes = new float[4];

        movementAction.action.actionMap.Enable();
    }


    public void OnPollInput(CallbackPollInput callback) {

        Vector2 stick = movementAction.action.ReadValue<Vector2>();
        Vector2 normalizedJoystick = stick.normalized;
        //TODO: changeable deadzone?
        bool up = Vector2.Dot(normalizedJoystick, Vector2.up) > 0.6f;
        bool down = Vector2.Dot(normalizedJoystick, Vector2.down) > 0.6f;
        bool left = Vector2.Dot(normalizedJoystick, Vector2.left) > 0.4f;
        bool right = Vector2.Dot(normalizedJoystick, Vector2.right) > 0.4f;

        directionTimes[0] = left ? Time.time : 0;
        directionTimes[1] = up ? Time.time : 0;
        directionTimes[2] = right ? Time.time : 0;
        directionTimes[3] = down ? Time.time : 0;

        sbyte target = -1;
        float timeMax = 0;
        for (int i = 0; i < directionTimes.Length; i++) {
            if (directionTimes[i] > timeMax) {
                target = (sbyte) i;
                timeMax = directionTimes[i];
            }
        }

        Quantum.Input input = new() {
            TargetDirection = target,
            Bomb = bombAction.action.ReadValue<float>() > 0.5f
        };

        callback.SetInput(input, Photon.Deterministic.DeterministicInputFlags.Repeatable);
    }
}