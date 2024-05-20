using Quantum;
using System;
using TMPro;
using UnityEngine;

public unsafe class TimerUpdater : QuantumCallbacks {

    [SerializeField] private TMP_Text text;

    public void OnValidate() {
        if (!text) {
            text = GetComponent<TMP_Text>();
        }
    }

    public override void OnUpdateView(QuantumGame game) {
        Frame f = game.Frames.Predicted;
        float secondsRemaining = f.Global->Timer.AsFloat;

        if (secondsRemaining < 10) {
            text.text = secondsRemaining.ToString("0.00");
        } else if (secondsRemaining < 60) {
            text.text = secondsRemaining.ToString("0.0");
        } else {
            text.text = TimeSpan.FromSeconds(secondsRemaining).ToString(@"m\:ss");
        }
    }
}