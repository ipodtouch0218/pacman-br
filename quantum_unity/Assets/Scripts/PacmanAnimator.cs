using Quantum;
using System;
using UnityEngine;

public unsafe class PacmanAnimator : QuantumCallbacks {

    public static event Action<PacmanAnimator> OnPacmanCreated;

    [SerializeField] public EntityView entity;
    [SerializeField] private float blinkSpeedPerSecond = 30, moveAnimationSpeed = 5f, deathAnimationSpeed = 8f, deathDelay = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem respawnParticles, sparkleParticles;

    [SerializeField] private Sprite[] movementSprites, scaredMovementSprites, deathSprites;

    private bool invulnerable;
    private float invulnerableTimer;
    private float blinkSpeedPeriod;
    private bool dead;
    private float deathAnimationTimer, deathParticlesTimer;

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
        QuantumEvent.Subscribe<EventCharacterEaten>(this, OnCharacterEaten);
        QuantumEvent.Subscribe<EventGameFreeze>(this, OnGameFreeze);
        QuantumEvent.Subscribe<EventGameUnfreeze>(this, OnGameUnfreeze);

        blinkSpeedPeriod = 1 / blinkSpeedPerSecond;
        OnPacmanCreated?.Invoke(this);
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

            if (deathParticlesTimer > 0) {
                if ((deathParticlesTimer -= Time.deltaTime) <= 0) {
                    respawnParticles.Play();
                }
            }
        }
    }

    public override void OnUpdateView(QuantumGame game) {
        if (!game.Frames.Predicted.TryGet(entity.EntityRef, out PacmanPlayer pac) || !game.Frames.Predicted.TryGet(entity.EntityRef, out GridMover mover)) {
            return;
        }

        if (!dead) {
            var sprites = game.Frames.Predicted.Global->PowerPelletDuration > 0 ? (pac.HasPowerPellet ? movementSprites : scaredMovementSprites) : movementSprites;
            int spritesPerCycle = sprites.Length / 4;
            int index = Mathf.FloorToInt(Mathf.PingPong(mover.DistanceMoved.AsFloat * moveAnimationSpeed, spritesPerCycle) - 0.001f + (spritesPerCycle * mover.Direction));
            spriteRenderer.sprite = sprites[index];
        }
    }

    public void OnPacmanKilled(EventPacmanKilled e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        dead = true;
        deathAnimationTimer = 0;
        deathParticlesTimer = e.RespawnInSeconds.AsFloat - 2;
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

    public void OnCharacterEaten(EventCharacterEaten e) {
        if (e.Pacman != entity.EntityRef) {
            return;
        }

        sparkleParticles.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360f), Vector3.up) * sparkleParticles.transform.rotation;
        sparkleParticles.Play();
    }

    public void OnGameFreeze(EventGameFreeze e) {
        if (respawnParticles.isPlaying) {
            respawnParticles.Pause();
        }
        if (deathParticlesTimer > 0) {
            deathParticlesTimer += e.Duration.AsFloat;
        }
    }

    public void OnGameUnfreeze(EventGameUnfreeze e) {
        if (respawnParticles.isPaused) {
            respawnParticles.Play();
        }
    }
}