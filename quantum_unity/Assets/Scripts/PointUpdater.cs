using Quantum;
using TMPro;
using UnityEngine;

public class PointUpdater : QuantumCallbacks {

    [SerializeField] public EntityView entity;
    [SerializeField] private TMP_Text text;

    [SerializeField] private GameObject powerMeter;
    [SerializeField] private SlicedFilledImage powerMeterImage;

    public void Start() {
        QuantumEvent.Subscribe<EventPacmanScored>(this, OnPacmanScored);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
    }

    public override void OnUpdateView(QuantumGame game) {
        var f = game.Frames.Predicted;
        if (!f.TryGet(entity.EntityRef, out PacmanPlayer pacman)) {
            return;
        }

        // TODO: change
        powerMeterImage.fillAmount = pacman.PowerPelletTimer.AsFloat / 10f;
    }

    public void OnPacmanScored(EventPacmanScored e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        text.text = e.TotalPoints.ToString().PadLeft(6, '0');
    }

    public void OnPowerPelletEat(EventPowerPelletEat e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        powerMeter.SetActive(true);
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        powerMeter.SetActive(false);
    }
}