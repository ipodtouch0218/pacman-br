using Quantum;
using System.Collections.Generic;
using UnityEngine;

public class SoundHandler : QuantumCallbacks {

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LoopingAudioClip defaultSound, scaredSound, eatenSound;

    private LoopingAudioClip currentClip;
    private readonly HashSet<EntityRef> eatenGhosts = new();
    private bool powerPellet;

    public void Start() {
        QuantumEvent.Subscribe<EventGameFreeze>(this, OnGameFreeze);
        QuantumEvent.Subscribe<EventGameUnfreeze>(this, OnGameUnfreeze);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEaten);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
        QuantumEvent.Subscribe<EventGhostStateChanged>(this, OnGhostChangeState);

        ChangeAudioClip();
    }

    public void Update() {
        LoopingAudioClip.Update(audioSource, currentClip);
    }

    public void OnGameFreeze(EventGameFreeze e) {
        audioSource.Stop();
    }

    public void OnGameUnfreeze(EventGameUnfreeze e) {
        audioSource.Play();
    }

    public void OnPowerPelletEaten(EventPowerPelletEat e) {
        powerPellet = true;
        ChangeAudioClip();
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        powerPellet = false;
        ChangeAudioClip();
    }

    public void OnGhostChangeState(EventGhostStateChanged e) {
        if (e.State == GhostState.Eaten) {
            eatenGhosts.Add(e.Entity);
        } else {
            eatenGhosts.Remove(e.Entity);
        }
        ChangeAudioClip();
    }

    private void ChangeAudioClip() {
        LoopingAudioClip oldClip = currentClip;
        if (eatenGhosts.Count > 0) {
            currentClip = eatenSound;
        } else if (powerPellet) {
            currentClip = scaredSound;
        } else {
            currentClip = defaultSound;
        }

        bool play = oldClip != currentClip && audioSource.isPlaying;
        audioSource.clip = currentClip.Clip;
        if (play) {
            audioSource.Play();
        }
    }

}