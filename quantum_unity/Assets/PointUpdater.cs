using Quantum;
using TMPro;
using UnityEngine;

public class PointUpdater : MonoBehaviour {

    [SerializeField] private TMP_Text text;

    public void Start() {
        QuantumEvent.Subscribe<EventPacmanScored>(this, OnPacmanScored);
    }

    public void OnPacmanScored(EventPacmanScored e) {
        text.text = e.TotalPoints.ToString().PadLeft(6, '0');
    }
}