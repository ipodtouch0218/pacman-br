using System.Collections.Generic;
using Quantum;
using UnityEngine;

public class ScorePanel : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private PointUpdater template;

    //---Private Variables
    private readonly HashSet<PointUpdater> pointUpdaters = new();

    public void Start() {
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
    }

    public void OnEnable() {
        PacmanAnimator.OnPacmanCreated += OnPacmanCreated;
    }

    public void OnDisable() {
        PacmanAnimator.OnPacmanCreated -= OnPacmanCreated;
    }

    public void OnGameStarting(EventGameStarting e) {
        foreach (var updater in pointUpdaters) {
            updater.transform.SetParent(transform);
        }
    }


    private void OnPacmanCreated(QuantumGame game, PacmanAnimator pacman) {
        PointUpdater newPointUpdater = Instantiate(template, transform);
        newPointUpdater.Initialize(game.Frames.Predicted, pacman);
        pointUpdaters.Add(newPointUpdater);
    }
}