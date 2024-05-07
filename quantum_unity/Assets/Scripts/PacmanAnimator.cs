using Quantum;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public unsafe class PacmanAnimator : QuantumCallbacks {

    public static event Action<PacmanAnimator> OnPacmanCreated;

    [SerializeField] public EntityView entity;
    [SerializeField] private float blinkSpeedPerSecond = 30, moveAnimationSpeed = 5f, deathAnimationSpeed = 8f, deathDelay = 0.5f, scaredBlinkStart = 3, scaredBlinkSpeedPerSecond = 2;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem respawnParticles, sparkleParticles;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private Sprite[] movementSprites, deathSprites;
    [SerializeField] private AudioClip wa, ka, death;
    [SerializeField] private AudioClip[] eatClips;

    [SerializeField] private Color scaredColor;
    [SerializeField] private Color[] playerColors;
    [SerializeField] private Light2D light;

    private bool invulnerable;
    private float invulnerableTimer;
    private float blinkSpeedPeriod;
    private bool dead;
    private float deathAnimationTimer, deathParticlesTimer;
    private bool waSound;

    private MaterialPropertyBlock mpb;


    public void OnValidate() {
        if (!entity) {
            entity = GetComponent<EntityView>();
        }
        if (!spriteRenderer) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (!audioSource) {
            audioSource = GetComponent<AudioSource>();
        }
        if (!light) {
            light = GetComponent<Light2D>();
        }
    }

    public void Awake() {
        QuantumEvent.Subscribe<EventPacmanKilled>(this, OnPacmanKilled);
        QuantumEvent.Subscribe<EventPacmanRespawned>(this, OnPacmanRespawned);
        QuantumEvent.Subscribe<EventPacmanVulnerable>(this, OnPacmanVulnerable);
        QuantumEvent.Subscribe<EventCharacterEaten>(this, OnCharacterEaten);
        QuantumEvent.Subscribe<EventGameFreeze>(this, OnGameFreeze);
        QuantumEvent.Subscribe<EventGameUnfreeze>(this, OnGameUnfreeze);
        QuantumEvent.Subscribe<EventPelletEat>(this, OnPelletEat);

        blinkSpeedPeriod = 1 / blinkSpeedPerSecond;
        OnPacmanCreated?.Invoke(this);

        mpb = new();
        spriteRenderer.GetPropertyBlock(mpb);
    }

    public void Update() {
        if (invulnerable) {
            invulnerableTimer += Time.deltaTime;
            spriteRenderer.enabled = (invulnerableTimer % (blinkSpeedPeriod * 2)) < blinkSpeedPeriod;
        }

        if (dead) {
            // Play animation...
            bool playDeathSound = deathAnimationTimer < deathDelay;
            deathAnimationTimer += Time.deltaTime;
            playDeathSound &= deathAnimationTimer >= deathDelay;

            if (playDeathSound) {
                audioSource.PlayOneShot(death);
            }

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

        Color playerColor = Color.gray;
        if (game.Frames.Predicted.TryGet(entity.EntityRef, out PlayerLink pl)) {
            playerColor = playerColors[(pl.Player._index - 1) % playerColors.Length];
        }

        float timeSinceStart = game.Frames.Predicted.Global->TimeSinceGameStart.AsFloat;
        float pelletTimeRemaining = pac.PowerPelletTimer.AsFloat;
        float scaredBlinkPeriod = 1f / (scaredBlinkSpeedPerSecond * (pelletTimeRemaining < 1 ? 2 : 1));
        bool scaredFlash = (pelletTimeRemaining < scaredBlinkStart) && (timeSinceStart % scaredBlinkPeriod < (scaredBlinkPeriod / 2));

        bool otherPlayerHasPellet = false;
        var filter = game.Frames.Predicted.Filter<PacmanPlayer>();
        while (filter.Next(out EntityRef otherEntity, out PacmanPlayer otherPac)) {
            if (otherEntity == entity.EntityRef) {
                continue;
            }

            if (otherPac.HasPowerPellet) {
                otherPlayerHasPellet = true;
                break;
            }
        }

        if (otherPlayerHasPellet && (!pac.HasPowerPellet || scaredFlash)) {
            // Scared
            mpb.SetColor("_BaseColor", scaredColor);
            mpb.SetColor("_OutlineColor", playerColor);
            light.color = scaredColor;
        } else {
            // Other
            mpb.SetColor("_BaseColor", playerColor);
            mpb.SetColor("_OutlineColor", Color.white);
            light.color = playerColor;
        }
        spriteRenderer.SetPropertyBlock(mpb);

        if (!dead) {
            int spritesPerCycle = movementSprites.Length / 4;
            int offset = spritesPerCycle * mover.Direction;
            int index;
            if (mover.IsStationary) {
                index = Mathf.FloorToInt(spritesPerCycle / 2) + offset;
            } else {
                index = Mathf.FloorToInt(Mathf.PingPong(mover.DistanceMoved.AsFloat * moveAnimationSpeed, spritesPerCycle) - 0.001f) + offset;
            }
            spriteRenderer.sprite = movementSprites[index];
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

        audioSource.PlayOneShot(eatClips[Mathf.Min(e.Combo - 1, eatClips.Length - 1)]);
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

    public void OnPelletEat(EventPelletEat e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        audioSource.PlayOneShot(waSound ? wa : ka);
        waSound = !waSound;
    }
}