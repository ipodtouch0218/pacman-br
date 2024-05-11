using Quantum;
using Quantum.Util;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public unsafe class GhostAnimator : QuantumCallbacks {

    //---Serialized Variables
    [SerializeField] private EntityView entity;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] movementSprites, scaredSprites, eatenSprites;
    [SerializeField] private float animationSpeed = 4, flashTimeRemaining = 5, flashesPerSecond = 1;

    [SerializeField] private ParticleSystem trailParticle;
    [SerializeField] private GameObject eyeTrails;

    [SerializeField] private Light2D ghostLight;
    [SerializeField] private Color scaredLightColor = Color.blue;

    //---Private Variables
    private Sprite[] currentSprites;
    private Color originalLightColor;

    private ParticleSystemRenderer trailRenderer;
    private MaterialPropertyBlock trailMpb;

    public void OnValidate() {
        if (!entity) {
            entity = GetComponentInChildren<EntityView>();
        }
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

        originalLightColor = ghostLight.color;
        eyeTrails.SetActive(false);
        trailRenderer = trailParticle.GetComponent<ParticleSystemRenderer>();
        trailRenderer.GetPropertyBlock(trailMpb = new());
    }

    public void Initialized(QuantumGame game) {
        Ghost ghost = game.Frames.Predicted.Get<Ghost>(entity.EntityRef);
        OnGhostStateChanged(new EventGhostStateChanged() {
            Frame = game.Frames.Predicted,
            Entity = entity.EntityRef,
            Game = game,
            State = ghost.State,
            Tick = game.Frames.Predicted.Number
        });
    }

    public override void OnUpdateView(QuantumGame game) {
        if (!game.Frames.Predicted.TryGet(entity.EntityRef, out GridMover mover)) {
            return;
        }

        if (currentSprites == scaredSprites) {
            float scaredTimeRemaining = game.Frames.Predicted.Global->PowerPelletRemainingTime.AsFloat;
            float flashPeriod = 1f / (flashesPerSecond * (scaredTimeRemaining < 1 ? 2 : 1));
            int offset = ((scaredTimeRemaining < flashTimeRemaining) && (scaredTimeRemaining % flashPeriod) < (flashPeriod / 2)) ? 2 : 0;
            int index = (int) ((mover.DistanceMoved.AsFloat * animationSpeed) % (currentSprites.Length / 2)) + offset;
            spriteRenderer.sprite = currentSprites[index];
        } else {
            int spritesPerDirection = currentSprites.Length / 4;
            int index = (int) ((mover.DistanceMoved.AsFloat * animationSpeed) % spritesPerDirection) + mover.Direction * spritesPerDirection;
            spriteRenderer.sprite = currentSprites[index];
        }
    }

    public void OnGhostStateChanged(EventGhostStateChanged e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        var emission = trailParticle.emission;
        switch (e.State) {
        case GhostState.Chase:
            currentSprites = movementSprites;
            ghostLight.color = originalLightColor;
            trailMpb.SetColor("_AdditiveColor", originalLightColor);
            eyeTrails.SetActive(false);
            break;
        case GhostState.Scared:
            currentSprites = scaredSprites;
            ghostLight.color = scaredLightColor;
            trailMpb.SetColor("_AdditiveColor", scaredLightColor);
            eyeTrails.SetActive(false);
            break;
        case GhostState.Eaten:
            currentSprites = eatenSprites;
            ghostLight.color = originalLightColor;
            emission.enabled = false;
            eyeTrails.SetActive(true);
            break;
        }
        trailRenderer.SetPropertyBlock(trailMpb);
    }

    public void OnGridMoverReachedCenterOfTile(EventGridMoverReachedCenterOfTile e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        Ghost ghost = e.Game.Frames.Predicted.Get<Ghost>(e.Entity);
        if (ghost.GhostHouseState != GhostHouseState.NotInGhostHouse) {
            return;
        }

        Vector3 worldPosition = FPVectorUtils.CellToWorld(e.Tile, e.Game.Frames.Predicted).XOY.ToUnityVector3();
        trailParticle.transform.position = worldPosition;
        trailParticle.Emit(1);
    }
}