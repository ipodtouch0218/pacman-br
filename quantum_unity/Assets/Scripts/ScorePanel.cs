using UnityEngine;

public class ScorePanel : MonoBehaviour {

    [SerializeField] private PointUpdater template;

    public void OnEnable() {
        PacmanAnimator.OnPacmanCreated += PacmanAnimator_OnPacmanCreated;
    }

    public void OnDisable() {
        PacmanAnimator.OnPacmanCreated -= PacmanAnimator_OnPacmanCreated;
    }

    private void PacmanAnimator_OnPacmanCreated(PacmanAnimator pacman) {
        PointUpdater newPointUpdater = Instantiate(template, transform);
        newPointUpdater.entity = pacman.entity;
        newPointUpdater.gameObject.SetActive(true);
    }
}