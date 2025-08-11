using Quantum;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public unsafe class GridColorHandler : QuantumSceneViewComponent {

    //---Serialized Variables
    [SerializeField] private FullScreenPassRendererFeature fullscreenPass;
    [SerializeField] private Material fullscreenWaveMaterial;
    [SerializeField] private TilemapRenderer tilemapRenderer;
    [SerializeField] private Gradient gradient;

    [SerializeField] private float gradientStartSeconds = 5f;
    [SerializeField] private float maxWaveIntensity = 0.07f;
    [SerializeField] private float screenshakeDuration = 0.5f, screenshakePeriod = 0.2f, screenshakeIntensity = 0.1f;

    //---Private Variables
    private MaterialPropertyBlock mpb;

    private float screenshakeTimer;
    private Vector2 screenshakeDirection;
    private Material fullscreenMat;

    private void Start() {
        mpb = new();
        tilemapRenderer.GetPropertyBlock(mpb);

        QuantumEvent.Subscribe<EventEntityEaten>(this, OnEntityEaten);
        QuantumEvent.Subscribe<EventPacmanUseBomb>(this, OnPacmanUseBomb);

        fullscreenPass.passMaterial = fullscreenMat = new Material(fullscreenWaveMaterial);
        fullscreenPass.SetActive(true);
    }

    public override void OnDisable() {
        base.OnDisable();
        fullscreenPass.SetActive(false);
    }

    public void OnDestroy() {
        Destroy(fullscreenMat);
        fullscreenPass.SetActive(false);
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

    public override unsafe void OnUpdateView() {
        Frame f = PredictedFrame;

        float remainingTime = f.Global->PowerPelletRemainingTime.AsFloat;
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

        fullscreenMat.SetFloat("_Wave_Intensity", (1f - percentage) * maxWaveIntensity);

        mpb.SetColor("_Color", gradient.Evaluate(percentage));
        tilemapRenderer.SetPropertyBlock(mpb);
    }

    public void OnEntityEaten(EventEntityEaten e) {
        if (!VerifiedFrame.Unsafe.TryGetPointer(e.Pacman, out GridMover* mover)) {
            return;
        }

        /*
        if (!e.Frame.TryGet(e.Pacman, out PlayerLink playerLink) || !e.Game.GetLocalPlayers().Contains(playerLink.Player)) {
            return;
        }
        */

        screenshakeTimer = screenshakeDuration;
        screenshakeDirection = mover->DirectionAsVector2().ToUnityVector2();
    }

    public void OnPacmanUseBomb(EventPacmanUseBomb e) {
        screenshakeTimer = screenshakeDuration;
        screenshakeDirection = Vector2.down;
    }
}
