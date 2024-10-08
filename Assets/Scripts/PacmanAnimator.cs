using Quantum;
using Quantum.Pacman.Ghost;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public unsafe class PacmanAnimator : QuantumCallbacks {

    public static event Action<QuantumGame, PacmanAnimator> OnPacmanCreated;

    public Color PlayerColor { get; private set; }

    //---Serialized Variables
    [SerializeField] public QuantumEntityView entity;
    [SerializeField] private float blinkSpeedPerSecond = 30, moveAnimationSpeed = 5f, deathAnimationSpeed = 8f, deathDelay = 0.5f, scaredBlinkStart = 3, scaredBlinkSpeedPerSecond = 2;
    [SerializeField] private SpriteRenderer spriteRenderer, arrowRenderer;
    [SerializeField] private ParticleSystem respawnParticles, sparkleParticles, eatParticles, sparkParticles, bombParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GiveBomb giveBombPrefab;

    [SerializeField] private Sprite[] movementSprites, deathSprites;
    [SerializeField] private AudioClip wa, ka, death, powerPellet, bombCollect, bombUse;
    [SerializeField] private AudioClip[] eatClips;

    [SerializeField] private Color scaredColor;
    [SerializeField] private Color[] playerColors;
    [SerializeField] private Light2D light;

    [SerializeField] private TrailRenderer bombTrail;
    [SerializeField] private TMP_Text bombText;

    //---Private Variables
    private float blinkSpeedPeriod;
    private bool dead;
    private float deathAnimationTimer, deathParticlesTimer;
    private bool waSound;

    private Coroutine fadeBombTextCoroutine;
    private MaterialPropertyBlock mpb;


    public void OnValidate() {
        if (!entity) {
            entity = GetComponent<QuantumEntityView>();
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
        QuantumEvent.Subscribe<EventPelletEat>(this, OnPelletEat);
        QuantumEvent.Subscribe<EventFruitEaten>(this, OnFruitEaten);
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        QuantumEvent.Subscribe<EventPacmanCollectBomb>(this, OnCollectBomb);
        QuantumEvent.Subscribe<EventPacmanUseBomb>(this, OnUseBomb);
        QuantumEvent.Subscribe<EventPacmanLandBombJump>(this, OnLandBombJump);

        blinkSpeedPeriod = 1 / blinkSpeedPerSecond;

        mpb = new();
        spriteRenderer.GetPropertyBlock(mpb);
    }

    public void Initialize(QuantumGame game) {
        PlayerColor = Utils.GetPlayerColor(game.Frames.Predicted, entity.EntityRef);

        var main = respawnParticles.main;
        main.startColor = PlayerColor;
        arrowRenderer.color = PlayerColor;
        OnPacmanCreated?.Invoke(game, this);

        Color c = bombText.color;
        c.a = 0;
        bombText.color = c;
    }

    public void Update() {
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
                    var emission = sparkleParticles.emission;
                    emission.enabled = true;
                }
            }
        }
    }

    public override void OnUpdateView(QuantumGame game) {
        var f = game.Frames.Predicted;
        if (!f.TryGet(entity.EntityRef, out PacmanPlayer pac)
            || !f.TryGet(entity.EntityRef, out GridMover mover)) {
            return;
        }

        if (pac.Invincibility > 0) {
            spriteRenderer.enabled = (pac.Invincibility.AsFloat % (blinkSpeedPeriod * 2)) < blinkSpeedPeriod;
        } else {
            spriteRenderer.enabled = true;
        }

        float timeSinceStart = (f.Number * f.DeltaTime).AsFloat;
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
            mpb.SetColor("_OutlineColor", PlayerColor);
            light.color = scaredColor;
        } else {
            // Other
            mpb.SetColor("_BaseColor", PlayerColor);
            mpb.SetColor("_OutlineColor", Color.white);
            light.color = PlayerColor;
        }
        spriteRenderer.SetPropertyBlock(mpb);

        if (!dead) {
            int spritesPerCycle = movementSprites.Length / 4;
            int offset = spritesPerCycle * mover.Direction;
            int index;
            if (mover.IsStationary) {
                index = Mathf.FloorToInt(spritesPerCycle / 2) + offset;
            } else {
                index = Mathf.FloorToInt(Mathf.PingPong(mover.DistanceMoved.AsFloat * moveAnimationSpeed, spritesPerCycle)) + offset;
            }
            spriteRenderer.sprite = movementSprites[Mathf.Clamp(index, 0, movementSprites.Length - 1)];
        }

        bombTrail.emitting = pac.BombTravelTimer > 0;
        UpdateSparks(f);
    }

    private void UpdateSparks(Frame frame) {

        var emission = sparkParticles.emission;
        var shape = sparkParticles.shape;

        PacmanPlayer pacman = frame.Get<PacmanPlayer>(entity.EntityRef);
        GridMover* mover = frame.Unsafe.GetPointer<GridMover>(entity.EntityRef);

        if (!frame.SystemIsEnabledInHierarchy<GridMovementSystem>() || pacman.IsDead || mover->IsLocked || mover->IsStationary || mover->FreezeTime > 0) {
            emission.enabled = false;
            return;
        }

        if (!frame.TryGet(entity.EntityRef, out PlayerLink pl)) {
            emission.enabled = false;
            return;
        }

        Quantum.Input input = *frame.GetPlayerInput(pl.Player);
        bool left = input.TargetDirection == 0;
        bool up = input.TargetDirection == 1;
        bool right = input.TargetDirection == 2;
        bool down = input.TargetDirection == 3;

        switch (mover->Direction) {
        case 0:
            // Moving Left
            // Check for Up and Down
            emission.enabled = up ^ down;
            if (up) {
                sparkParticles.transform.SetLocalPositionAndRotation(new(0, 0, 0.5f), Quaternion.Euler(90, 0, 45));
            } else {
                sparkParticles.transform.SetLocalPositionAndRotation(new(0, 0, -0.5f), Quaternion.Euler(90, 0, 90));
            }
            break;
        case 1:
            // Moving Up
            // Check for Left and Right
            emission.enabled = left ^ right;
            if (left) {
                sparkParticles.transform.SetLocalPositionAndRotation(new(-0.5f, 0, 0), Quaternion.Euler(90, 0, 0));
            } else {
                sparkParticles.transform.SetLocalPositionAndRotation(new(0.5f, 0, 0), Quaternion.Euler(90, 0, -45));
            }
            break;
        case 2:
            // Moving Right
            // Check for Up and Down
            emission.enabled = up ^ down;
            if (up) {
                sparkParticles.transform.SetLocalPositionAndRotation(new(0, 0, 0.5f), Quaternion.Euler(90, 0, -90));
            } else {
                sparkParticles.transform.SetLocalPositionAndRotation(new(0, 0, -0.5f), Quaternion.Euler(90, 0, -135));
            }
            break;
        case 3:
            // Moving Down
            // Check for Left and Right
            emission.enabled = left ^ right;
            if (left) {
                sparkParticles.transform.SetLocalPositionAndRotation(new(-0.5f, 0, 0), Quaternion.Euler(90, 0, 135));
            } else {
                sparkParticles.transform.SetLocalPositionAndRotation(new(0.5f, 0, 0), Quaternion.Euler(90, 0, 180));
            }
            break;
        }
    }

    public void ShowBombCount(Frame f, bool addOne) {
        int bombs = f.Get<PacmanPlayer>(entity.EntityRef).Bombs;
        if (addOne && bombs > 0) {
            bombText.text = "<sprite=0>X" + (bombs - 1) + " + 1";
        } else {
            bombText.text = "<sprite=0>X" + bombs;
        }

        if (addOne) {
            audioSource.PlayOneShot(bombCollect);
        }

        Color c = bombText.color;
        c.a = 1;
        bombText.color = c;
        if (fadeBombTextCoroutine != null) {
            StopCoroutine(fadeBombTextCoroutine);
        }
        fadeBombTextCoroutine = StartCoroutine(FadeText(0.3f, 0.7f));
    }

    private IEnumerator FadeText(float fadeTime, float delay) {
        yield return new WaitForSeconds(delay);
        float totalTime = fadeTime;

        Color c = bombText.color;
        while ((fadeTime -= Time.deltaTime) > 0) {
            c.a = fadeTime / totalTime;
            bombText.color = c;
            yield return null;
        }

        c.a = 0;
        bombText.color = c;
    }

    public void OnGameStarting(EventGameStarting e) {
        dead = false;
        spriteRenderer.enabled = true;
        var emission = sparkleParticles.emission;
        emission.enabled = false;
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

        spriteRenderer.enabled = true;
        dead = false;
    }

    public void OnPacmanVulnerable(EventPacmanVulnerable e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        spriteRenderer.enabled = true;
        var emission = sparkleParticles.emission;
        emission.enabled = false;
    }

    public void OnCharacterEaten(EventCharacterEaten e) {
        if (e.Pacman != entity.EntityRef) {
            return;
        }

        int direction = e.Frame.Get<GridMover>(e.Pacman).Direction;
        Vector3 newForward = GridMover.DirectionToVector(direction).XOY.ToUnityVector3();
        eatParticles.transform.rotation = Quaternion.LookRotation(newForward, Vector3.up);
        eatParticles.Play();

        audioSource.PlayOneShot(eatClips[Mathf.Min(e.Combo - 1, eatClips.Length - 1)]);
    }

    public void OnPelletEat(EventPelletEat e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        audioSource.PlayOneShot(waSound ? wa : ka);
        waSound = !waSound;
    }

    public void OnFruitEaten(EventFruitEaten e) {
        if (e.Pacman != entity.EntityRef) {
            return;
        }

        audioSource.PlayOneShot(powerPellet);
    }

    public void OnCollectBomb(EventPacmanCollectBomb e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        GiveBomb newBomb = Instantiate(giveBombPrefab);
        newBomb.Initialize(this);
    }

    public void OnUseBomb(EventPacmanUseBomb e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        audioSource.PlayOneShot(bombUse);
        bombParticles.Play();
    }

    public void OnLandBombJump(EventPacmanLandBombJump e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        bombParticles.Play(false);
        ShowBombCount(e.Game.Frames.Predicted, false);
    }
}