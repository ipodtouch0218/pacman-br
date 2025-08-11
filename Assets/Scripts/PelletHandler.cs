using Photon.Deterministic;
using Quantum;
using Quantum.Collections;
using Quantum.Util;
using System.Collections.Generic;
using UnityEngine;

public unsafe class PelletHandler : QuantumSceneViewComponent {

    //---Serialized Variables
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameObject smallPelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;

    [SerializeField] private GameObject powerPelletCollectPrefab;
    [SerializeField] private AudioClip powerPelletClip;

    //---Private Variables
    private readonly Dictionary<int, GameObject> pelletGOs = new();

    public void OnValidate() {
        this.SetIfNull(ref audioSource);
    }

    public void Start() {
        QuantumEvent.Subscribe<EventPelletRespawn>(this, OnEventPelletRespawn);
        QuantumEvent.Subscribe<EventPelletEat>(this, OnEventPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnEventPowerPelletEat);
    }

    public override void OnActivate(Frame f) {
        OnEventPelletRespawn(new() {
            Game = Game
        });
    }

    public void OnEventPelletEat(EventPelletEat e) {
        Frame f = PredictedFrame;
        var map = (PacmanStageMapData) QuantumUnityDB.GetGlobalAsset(f.Map.UserAsset);
        int index = e.Tile.X.AsInt + (e.Tile.Y.AsInt * map.CurrentMazeData(f).Size.X.AsInt);

        if (!pelletGOs.TryGetValue(index, out GameObject pellet)) {
            return;
        }

        Destroy(pellet);
        pelletGOs.Remove(index);
    }

    public unsafe void OnEventPelletRespawn(EventPelletRespawn e) {
        DestroyPellets();

        var f = PredictedFrame;
        QDictionary<FPVector2, byte> pellets = f.ResolveDictionary(f.Global->PelletData);

        foreach ((FPVector2 cell, byte value) in pellets) {

            GameObject prefab = value switch {
                1 => smallPelletPrefab,
                2 => powerPelletPrefab,
                _ => null
            };

            if (!prefab) {
                continue;
            }

            GameObject newPellet = Instantiate(prefab, transform, true);
            newPellet.transform.position = FPVectorUtils.CellToWorld(cell, f).ToUnityVector3();
            pelletGOs.Add(FPVectorUtils.CellToIndex(cell, f), newPellet);
        }
    }

    public void OnEventPowerPelletEat(EventPowerPelletEat e) {
        audioSource.PlayOneShot(powerPelletClip);

        if (powerPelletCollectPrefab && VerifiedFrame.Unsafe.TryGetPointer(e.Entity, out Transform2D* transform)) {
            GameObject newPrefab = Instantiate(powerPelletCollectPrefab);
            newPrefab.transform.position = transform->Position.ToUnityVector3();
        }
    }

    private void DestroyPellets() {
        foreach ((var _, var pellet) in pelletGOs) {
            Destroy(pellet);
        }
        pelletGOs.Clear();
    }
}