using Quantum;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public unsafe class TimerUpdater : QuantumCallbacks {

    [SerializeField] private TMP_Text text;

    private Color originalColor;

    public void OnValidate() {
        if (!text) {
            text = GetComponent<TMP_Text>();
        }
    }

    public void Start() {
        QuantumEvent.Subscribe<EventTimerSecondPassed>(this, OnTimerSecondPassed);
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        originalColor = text.color;
    }

    public void UpdateText(Frame f) {
        float secondsRemaining = f.Global->Timer.AsFloat;

        if (secondsRemaining < 10) {
            text.text = secondsRemaining.ToString("0.00");
        } else if (secondsRemaining < 60) {
            text.text = secondsRemaining.ToString("0.0");
        } else {
            text.text = TimeSpan.FromSeconds(secondsRemaining).ToString(@"m\:ss");
        }
    }

    public override void OnUpdateView(QuantumGame game) {
        UpdateText(game.Frames.Predicted);
    }

    public void OnGameStarting(EventGameStarting e) {
        UpdateText(e.Game.Frames.Predicted);
    }

    public void OnTimerSecondPassed(EventTimerSecondPassed e) {
        if (e.SecondsRemaining >= 60 && e.SecondsRemaining % 60 == 1) {
            StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash() {
        WaitForSeconds delay = new(0.1f);

        for (int i = 0; i < 4; i++) {
            yield return delay;
            text.color = Color.white;
            yield return delay;
            text.color = originalColor;
        }
    }
}