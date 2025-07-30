using Quantum;
using System.Collections.Generic;
using UnityEngine;

public class MazeRefreshWave : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private List<AudioSource> audioSources;
    [SerializeField] private float minX = -10, maxX = 10;
    [SerializeField] private float speed = 10;

    //---Private Variables
    private bool fromLeft;
    private bool active;

    public void OnValidate() {
        if (audioSources?.Count == 0) {
            GetComponents(audioSources);
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
        if (!e.PlayEffect) {
            return;
        }

        if (fromLeft = e.WipeFromLeft) {
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