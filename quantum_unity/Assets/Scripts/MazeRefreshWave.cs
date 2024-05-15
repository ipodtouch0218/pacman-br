using Quantum;
using UnityEngine;

public class MazeRefreshWave : MonoBehaviour {

    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private float minX = -10, maxX = 10;
    [SerializeField] private float speed = 10;

    private bool fromLeft;
    private bool active;

    public void OnValidate() {
        if (audioSources == null || audioSources.Length == 0) {
            audioSources = GetComponents<AudioSource>();
        }
    }

    public void Start() {
        QuantumEvent.Subscribe<EventPelletRespawn>(this, OnPelletRespawn);
    }

    public void Update() {
        if (!active) {
            return;
        }

        transform.position += speed * Time.deltaTime * (fromLeft ? Vector3.right : Vector3.left);

        if (fromLeft) {
            if (transform.position.x > maxX) {
                active = false;
            }
        } else {
            if (transform.position.x < minX) {
                active = false;
            }
        }
    }

    public void OnPelletRespawn(EventPelletRespawn e) {
        if (fromLeft = (Random.Range(0, 2) == 0)) {
            // From left
            transform.position = Vector3.right * minX;
            transform.localScale = new(1, 1, 1);
        } else {
            // From right
            transform.position = Vector3.right * maxX;
            transform.localScale = new(-1, 1, 1);
        }

        active = true;
        foreach (AudioSource audioSource in audioSources) {
            audioSource.Play();
        }
    }
}