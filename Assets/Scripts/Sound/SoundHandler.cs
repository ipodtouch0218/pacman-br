using Photon.Deterministic;
using Quantum;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public unsafe class SoundHandler : QuantumSceneViewComponent {

    //---Serialized Variables
    [SerializeField] private AudioSource sfxSource, musicSource;
    [SerializeField] private LoopingAudioClip defaultSound, scaredSound, eatenSound;
    [SerializeField] private AudioClip startingClip, countdownClip, endClip;

    //---Private Variables
    private LoopingAudioClip currentClip;
    private readonly HashSet<EntityRef> eatenGhosts = new();
    private readonly HashSet<EntityRef> poweredUpPacman = new();

    public void Start() {
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEaten);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
        QuantumEvent.Subscribe<EventGhostStateChanged>(this, OnGhostChangeState);
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        QuantumEvent.Subscribe<EventGameStart>(this, OnGameStart);
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
        QuantumEvent.Subscribe<EventTimerSecondPassed>(this, OnTimerSecondPassed);
    }

    public override void OnUpdateView() {
        TryChangeAudioClip(PredictedFrame);
    }

    public void Update() {
        LoopingAudioClip.Update(sfxSource, currentClip);
    }

    public void OnPowerPelletEaten(EventPowerPelletEat e) {
        poweredUpPacman.Add(e.Entity);
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        poweredUpPacman.Remove(e.Entity);
    }

    public void OnGhostChangeState(EventGhostStateChanged e) {
        if (e.State == GhostState.Eaten) {
            eatenGhosts.Add(e.Entity);
        } else {
            eatenGhosts.Remove(e.Entity);
        }
    }

    public void OnGameStarting(EventGameStarting e) {
        sfxSource.PlayOneShot(startingClip);
    }

    public void OnGameStart(EventGameStart e) {
        currentClip = defaultSound;
        sfxSource.Play();
        musicSource.volume = 1;
        musicSource.Play();
    }

    public void OnGameEnd(EventGameEnd e) {
        sfxSource.Stop();
        sfxSource.PlayOneShot(endClip);

        StartCoroutine(FadeMusic(3, 1));
    }

    public void OnTimerSecondPassed(EventTimerSecondPassed e) {
        if (e.SecondsRemaining <= 10) {
            sfxSource.PlayOneShot(countdownClip);
        } else if (e.SecondsRemaining % 60 == 1) {
            sfxSource.PlayOneShot(endClip);
        }
    }

    private void TryChangeAudioClip(Frame f) {
        LoopingAudioClip oldClip = currentClip;
        bool scared = eatenGhosts.Any(g => f.Unsafe.GetPointer<Ghost>(g)->TimeSinceEaten >= FP._0_50);
        if (scared) {
            currentClip = eatenSound;
        } else if (poweredUpPacman.Count != 0) {
            currentClip = scaredSound;
        } else {
            currentClip = defaultSound;
        }

        bool play = oldClip != currentClip && sfxSource.isPlaying;
        sfxSource.clip = currentClip.Clip;
        if (play) {
            sfxSource.Play();
        }
    }

    private IEnumerator FadeMusic(float duration, float delay = 0) {
        yield return new WaitForSeconds(delay);

        float initialVolume = musicSource.volume;
        float timer = 0;
        while ((timer += Time.deltaTime) < duration) {
            musicSource.volume = Mathf.Lerp(initialVolume, 0, timer / duration);
            yield return null;
        }

        musicSource.volume = 0;
    }
}