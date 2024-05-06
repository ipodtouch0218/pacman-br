using Photon.Deterministic;
using Quantum;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoundHandler : QuantumCallbacks {

    //---Serialized Variables
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LoopingAudioClip defaultSound, scaredSound, eatenSound;

    //---Private Variables
    private LoopingAudioClip currentClip;
    private readonly HashSet<EntityRef> eatenGhosts = new();
    private bool powerPellet;

    public void Start() {
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEaten);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
        QuantumEvent.Subscribe<EventGhostStateChanged>(this, OnGhostChangeState);
    }

    public override void OnUpdateView(QuantumGame game) {
        TryChangeAudioClip(game.Frames.Predicted);
    }

    public void Update() {
        LoopingAudioClip.Update(audioSource, currentClip);
    }

    public void OnPowerPelletEaten(EventPowerPelletEat e) {
        powerPellet = true;
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        powerPellet = false;
    }

    public void OnGhostChangeState(EventGhostStateChanged e) {
        if (e.State == GhostState.Eaten) {
            eatenGhosts.Add(e.Entity);
        } else {
            eatenGhosts.Remove(e.Entity);
        }
    }

    private void TryChangeAudioClip(Frame f) {
        LoopingAudioClip oldClip = currentClip;
        bool scared = eatenGhosts.Any(g => {
            return f.Get<Ghost>(g).TimeSinceEaten >= FP._0_50;
        });
        if (scared) {
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