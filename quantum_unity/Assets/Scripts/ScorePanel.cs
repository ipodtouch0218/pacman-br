using Quantum;
using UnityEngine;

public class ScorePanel : MonoBehaviour {

    [SerializeField] private PointUpdater template;

    public void OnEnable() {
        PacmanAnimator.OnPacmanCreated += OnPacmanCreated;
    }

    public void OnDisable() {
        PacmanAnimator.OnPacmanCreated -= OnPacmanCreated;
    }

    private void OnPacmanCreated(QuantumGame game, PacmanAnimator pacman) {
        PointUpdater newPointUpdater = Instantiate(template, transform);
        newPointUpdater.Initialize(pacman);
    }
}