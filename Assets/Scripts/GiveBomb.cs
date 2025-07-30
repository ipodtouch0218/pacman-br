using Quantum;
using UnityEngine;

public class GiveBomb : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private float time = 0.6f;

    //---Private Variables
    private PacmanAnimator target;
    private float timer;
    private Vector3 origin;

    public void Initialize(PacmanAnimator target) {
        this.target = target;

        Camera cam = Camera.main;
        Vector3 spawnPos = cam.WorldToViewportPoint(target.transform.position);
        spawnPos.x += spawnPos.x < 0.5f ? -0.1f : 0.1f;
        spawnPos.y += 1;

        transform.position = origin = cam.ViewportToWorldPoint(spawnPos);
        timer = time;
    }

    public void Update() {
        if ((timer -= Time.deltaTime) <= 0) {
            target.ShowBombCount(QuantumRunner.Default.Game.Frames.Predicted, true);
            Destroy(gameObject);
        }

        float alpha = timer / time;
        alpha = 1 - (alpha * alpha * alpha);
        transform.position = Vector3.Lerp(origin, target.transform.position + new Vector3(0, 0.6f, 0), alpha);
    }
}
