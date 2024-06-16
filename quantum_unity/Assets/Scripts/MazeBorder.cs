using Quantum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MazeBorder : QuantumCallbacks {

    //---Properties
    private float Lifetime => timeBetweenSpawns * images.Length;
    private Color CurrentColor => playersWithPellet.Count == 0 ? normalColor : scaredColor;

    //---Serialized Variables
    [SerializeField] private Image[] images;
    [SerializeField] private float timeBetweenSpawns = 0.25f;
    [SerializeField] private AnimationCurve scaleCurve, alphaCurve;
    [SerializeField] private Color normalColor, scaredColor;

    //---Private Variables
    private readonly HashSet<EntityRef> playersWithPellet = new();
    private bool emitting;

    public void OnValidate() {
        if (images == null || images.Length == 0) {
            images = GetComponentsInChildren<Image>();
        }
    }

    public void Start() {
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        QuantumEvent.Subscribe<EventGameStart>(this, OnGameStart);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
    }

    public void Update() {
        float currentTime = Time.time;

        for (int i = 0; i < images.Length; i++) {
            Image image = images[i];
            float offset = timeBetweenSpawns * i;

            float imageTime = (currentTime + offset) % Lifetime;
            bool ended = (imageTime - Time.deltaTime) < 0;

            if (ended) {
                image.gameObject.SetActive(emitting);
                image.color = CurrentColor;
            }

            if (image.gameObject.activeSelf) {
                float scale = scaleCurve.Evaluate(imageTime);
                image.transform.localScale = new Vector3(scale, scale, 1);

                Color newColor = image.color;
                newColor.a = alphaCurve.Evaluate(imageTime);
                image.color = newColor;
            }
        }
    }


    public void OnGameStarting(EventGameStarting e) {
        emitting = false;
        playersWithPellet.Clear();
    }

    public void OnGameStart(EventGameStart e) {
        emitting = true;
    }

    public void OnPowerPelletEat(EventPowerPelletEat e) {
        playersWithPellet.Add(e.Entity);
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        playersWithPellet.Remove(e.Entity);
    }
}