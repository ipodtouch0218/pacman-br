using Quantum;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridColorHandler : QuantumCallbacks {

    [SerializeField] private TilemapRenderer tilemapRenderer;
    [SerializeField] private Gradient gradient;

    [SerializeField] private float gradientStartSeconds = 5f;
    [SerializeField] private float maxWaveIntensity = 0.07f;
    [SerializeField] private float screenshakeDuration = 0.5f, screenshakePeriod = 0.2f, screenshakeIntensity = 0.1f;

    private MaterialPropertyBlock mpb;

    private float screenshakeTimer;
    private Vector2 screenshakeDirection;

    private void Start() {
        mpb = new();
        tilemapRenderer.GetPropertyBlock(mpb);

        QuantumEvent.Subscribe<EventCharacterEaten>(this, OnCharacterEaten);
        QuantumEvent.Subscribe<EventPacmanUseBomb>(this, OnPacmanUseBomb);
    }

    public void Update() {
        if ((screenshakeTimer -= Time.deltaTime) > 0) {
            // what...
            float time = screenshakeDuration - screenshakeTimer;
            float screenshakeAmount = Mathf.Sin((2 * Mathf.PI * time) / screenshakePeriod) * ((screenshakeDuration - time) / screenshakeDuration) * screenshakeIntensity;
            transform.localPosition = screenshakeAmount * screenshakeDirection;
        } else {
            transform.localPosition = Vector2.zero;
        }
    }

    public override unsafe void OnUpdateView(QuantumGame game) {
        var globals = game.Frames.Predicted.Global;

        float remainingTime = globals->PowerPelletRemainingTime.AsFloat;
        float percentage;
        if (remainingTime > 0) {
            if (remainingTime < gradientStartSeconds) {
                percentage = 1f - (remainingTime / gradientStartSeconds);
            } else {
                percentage = 0;
            }
        } else {
            percentage = 1;
        }

        mpb.SetColor("_Color", gradient.Evaluate(percentage));
        mpb.SetFloat("_WaveIntensity", (1f - percentage) * maxWaveIntensity);
        tilemapRenderer.SetPropertyBlock(mpb);
    }

    public void OnCharacterEaten(EventCharacterEaten e) {
        if (!e.Frame.TryGet(e.Pacman, out GridMover mover)) {
            return;
        }

        /*
        if (!e.Frame.TryGet(e.Pacman, out PlayerLink playerLink) || !e.Game.GetLocalPlayers().Contains(playerLink.Player)) {
            return;
        }
        */

        screenshakeTimer = screenshakeDuration;
        screenshakeDirection = mover.DirectionAsVector2().ToUnityVector2();
    }

    public void OnPacmanUseBomb(EventPacmanUseBomb e) {
        screenshakeTimer = screenshakeDuration;
        screenshakeDirection = Vector2.down;
    }
}
