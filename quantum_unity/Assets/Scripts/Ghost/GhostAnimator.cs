using Quantum;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public unsafe class GhostAnimator : QuantumCallbacks {

    //---Serialized Variables
    [SerializeField] private EntityView entity;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] movementSprites, scaredSprites, eatenSprites;
    [SerializeField] private float animationSpeed = 4, flashTimeRemaining = 5, flashesPerSecond = 1;

    [SerializeField] private Light2D ghostLight;
    [SerializeField] private Color scaredLightColor = Color.blue;

    //---Private Variables
    private Sprite[] currentSprites;
    private Color originalLightColor;

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

        originalLightColor = ghostLight.color;
    }

    public override void OnUpdateView(QuantumGame game) {
        if (!game.Frames.Predicted.TryGet(entity.EntityRef, out GridMover mover)) {
            return;
        }

        if (currentSprites == scaredSprites) {
            float flashPeriod = 1f / flashesPerSecond;
            float scaredTimeRemaining = game.Frames.Predicted.Global->PowerPelletDuration.AsFloat;
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

        currentSprites = e.State switch {
            GhostState.Scared => scaredSprites,
            GhostState.Eaten => eatenSprites,
            GhostState.Chase or _ => movementSprites,
        };

        if (e.State == GhostState.Scared) {
            ghostLight.color = scaredLightColor;
        } else {
            ghostLight.color = originalLightColor;
        }
    }
}