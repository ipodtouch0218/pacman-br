using Quantum;
using TMPro;
using UnityEngine;

public class PointUpdater : MonoBehaviour {

    [SerializeField] public EntityView entity;
    [SerializeField] private TMP_Text text;

    public void Start() {
        QuantumEvent.Subscribe<EventPacmanScored>(this, OnPacmanScored);
    }

    public void OnPacmanScored(EventPacmanScored e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        text.text = e.TotalPoints.ToString().PadLeft(6, '0');
    }
}