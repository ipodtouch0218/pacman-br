using Quantum;
using UnityEngine;

public unsafe class PacmanAnimator : QuantumCallbacks {

    [SerializeField] private EntityView entity;
    [SerializeField] private float blinkSpeedPerSecond = 30, moveAnimationSpeed = 5f, deathAnimationSpeed = 8f, deathDelay = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite[] movementSprites, scaredMovementSprites, deathSprites;

    private bool invulnerable;
    private float invulnerableTimer;
    private float blinkSpeedPeriod;
    private bool dead;
    private float deathAnimationTimer;

    public void OnValidate() {
        if (!entity) {
            entity = GetComponent<EntityView>();
        }
        if (!spriteRenderer) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void Start() {
        QuantumEvent.Subscribe<EventPacmanKilled>(this, OnPacmanKilled);
        QuantumEvent.Subscribe<EventPacmanRespawned>(this, OnPacmanRespawned);
        QuantumEvent.Subscribe<EventPacmanVulnerable>(this, OnPacmanVulnerable);

        blinkSpeedPeriod = 1 / blinkSpeedPerSecond;
    }

    public void Update() {
        if (invulnerable) {
            invulnerableTimer += Time.deltaTime;
            spriteRenderer.enabled = (invulnerableTimer % (blinkSpeedPeriod * 2)) < blinkSpeedPeriod;
        }

        if (dead) {
            // Play animation...
            deathAnimationTimer += Time.deltaTime;
            int index = (int) ((deathAnimationTimer - deathDelay) * deathAnimationSpeed);
            if (index >= deathSprites.Length) {
                spriteRenderer.enabled = false;
            } else if (index >= 0) {
                spriteRenderer.sprite = deathSprites[index];
            }
        }
    }

    public override void OnUpdateView(QuantumGame game) {
        if (!game.Frames.Predicted.TryGet(entity.EntityRef, out PacmanPlayer pac)) {
            return;
        }

        if (!game.Frames.Predicted.TryGet(entity.EntityRef, out GridMover mover)) {
            return;
        }

        if (!dead) {
            var sprites = game.Frames.Predicted.Global->PowerPelletDuration > 0 ? (pac.HasPowerPellet ? movementSprites : scaredMovementSprites) : movementSprites;
            int spritesPerCycle = sprites.Length / 4;
            int index = Mathf.FloorToInt(Mathf.PingPong(mover.DistanceMoved.AsFloat * moveAnimationSpeed, spritesPerCycle) + (spritesPerCycle * mover.Direction));
            spriteRenderer.sprite = sprites[index];
        }
    }

    public void OnPacmanKilled(EventPacmanKilled e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }
        dead = true;
        deathAnimationTimer = 0;
    }

    public void OnPacmanRespawned(EventPacmanRespawned e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }
        invulnerable = true;
        invulnerableTimer = 0;
        spriteRenderer.enabled = true;
        dead = false;
    }

    public void OnPacmanVulnerable(EventPacmanVulnerable e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }
        invulnerable = false;
        spriteRenderer.enabled = true;
    }
}