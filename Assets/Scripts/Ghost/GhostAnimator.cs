using Quantum;
using Quantum.Util;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public unsafe class GhostAnimator : QuantumEntityViewComponent {

    //---Serialized Variables
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] movementSprites, scaredSprites, eatenSprites;
    [SerializeField] private float animationSpeed = 4, flashTimeRemaining = 5, flashesPerSecond = 1;

    [SerializeField] private ParticleSystem trailParticle;
    [SerializeField] private GameObject eyeTrail;
    [SerializeField] private TrailRenderer[] eyeTrails;

    [SerializeField] private Light2D ghostLight;
    [SerializeField] private Color scaredLightColor = Color.blue;

    //---Private Variables
    private Sprite[] currentSprites;
    private Color originalLightColor;

    private ParticleSystemRenderer trailRenderer;
    private MaterialPropertyBlock trailMpb;

    public void OnValidate() {
        if (!spriteRenderer) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (!ghostLight) {
            ghostLight = GetComponentInChildren<Light2D>();
        }
    }

    public void Start() {
        currentSprites = movementSprites;
        QuantumEvent.Subscribe<EventGhostStateChanged>(this, OnGhostStateChanged);
        QuantumEvent.Subscribe<EventGridMoverReachedCenterOfTile>(this, OnGridMoverReachedCenterOfTile);
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);

        originalLightColor = ghostLight.color;
        eyeTrail.SetActive(false);
        trailRenderer = trailParticle.GetComponent<ParticleSystemRenderer>();
        trailRenderer.GetPropertyBlock(trailMpb = new());
    }

    public override void OnActivate(Frame frame) {
        var ghost = PredictedFrame.Unsafe.GetPointer<Ghost>(EntityRef);
        OnGhostStateChanged(new EventGhostStateChanged() {
            Entity = EntityRef,
            Game = Game,
            State = ghost->State,
            Tick = PredictedFrame.Number
        });
    }

    public override void OnUpdateView() {
        Frame f = PredictedFrame;
        if (!f.Unsafe.TryGetPointer(EntityRef, out GridMover* mover)) {
            return;
        }

        if (currentSprites == scaredSprites) {
            float scaredTimeRemaining = f.Global->PowerPelletRemainingTime.AsFloat;
            float flashPeriod = 1f / (flashesPerSecond * (scaredTimeRemaining < 1 ? 2 : 1));
            int offset = ((scaredTimeRemaining < flashTimeRemaining) && (scaredTimeRemaining % flashPeriod) < (flashPeriod / 2)) ? 2 : 0;
            int index = (int) ((mover->DistanceMoved.AsFloat * animationSpeed) % (currentSprites.Length / 2)) + offset;
            spriteRenderer.sprite = currentSprites[index];
        } else {
            int spritesPerDirection = currentSprites.Length / 4;
            int index = (int) ((mover->DistanceMoved.AsFloat * animationSpeed) % spritesPerDirection) + mover->Direction * spritesPerDirection;
            spriteRenderer.sprite = currentSprites[index];
        }
    }

    public void OnGhostStateChanged(EventGhostStateChanged e) {
        if (e.Entity != EntityRef) {
            return;
        }

        var emission = trailParticle.emission;
        switch (e.State) {
        case GhostState.Chase:
            currentSprites = movementSprites;
            ghostLight.color = originalLightColor;
            trailMpb.SetColor("_AdditiveColor", originalLightColor);
            eyeTrail.SetActive(false);
            break;
        case GhostState.Scared:
            currentSprites = scaredSprites;
            ghostLight.color = scaredLightColor;
            trailMpb.SetColor("_AdditiveColor", scaredLightColor);
            eyeTrail.SetActive(false);
            break;
        case GhostState.Eaten:
            currentSprites = eatenSprites;
            ghostLight.color = originalLightColor;
            emission.enabled = false;
            eyeTrail.SetActive(true);
            break;
        }
        trailRenderer.SetPropertyBlock(trailMpb);
    }

    public void OnGridMoverReachedCenterOfTile(EventGridMoverReachedCenterOfTile e) {
        if (e.Entity != EntityRef) {
            return;
        }

        Frame f = PredictedFrame;
        var ghost = PredictedFrame.Unsafe.GetPointer<Ghost>(EntityRef);
        if (ghost->GhostHouseState != GhostHouseState.NotInGhostHouse) {
            return;
        }

        Vector3 worldPosition = FPVectorUtils.CellToWorld(e.Tile, f).ToUnityVector3();
        trailParticle.transform.position = worldPosition;
        trailParticle.Emit(1);
    }

    public void OnTeleportStateChanged(bool value) {
        foreach (var trail in eyeTrails) {
            trail.emitting = !value;
        }
        spriteRenderer.gameObject.SetActive(!value);
        ghostLight.gameObject.SetActive(!value);
    }

    public void OnGameStarting(EventGameStarting e) {
        var ghost = PredictedFrame.Unsafe.GetPointer<Ghost>(EntityRef);
        spriteRenderer.sprite = ghost->Mode switch {
            GhostTargetMode.Pinky => movementSprites[2],
            GhostTargetMode.Inky or GhostTargetMode.Clyde => movementSprites[6],
            _ => movementSprites[0],
        };
        OnGhostStateChanged(new EventGhostStateChanged() {
            Game = e.Game,
            State = ghost->State,
            Entity = EntityRef,
            Tick = PredictedFrame.Number,
        });
    }
}