using Photon.Deterministic;
using Quantum;
using Quantum.Collections;
using Quantum.Util;
using System.Collections.Generic;
using UnityEngine;

public class PelletHandler : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private MapCustomDataAsset mapData;
    [SerializeField] private GameObject smallPelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;

    [SerializeField] private AudioClip powerPelletClip;

    //---Private Variables
    private readonly Dictionary<int, GameObject> pelletGOs = new();

    public void OnValidate() {
        if (!audioSource) {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void Awake() {
        QuantumEvent.Subscribe<EventPelletRespawn>(this, OnEventPelletRespawn);
        QuantumEvent.Subscribe<EventPelletEat>(this, OnEventPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnEventPowerPelletEat);
    }

    public void OnEventPelletEat(EventPelletEat e) {
        int index = e.Tile.X.AsInt + e.Tile.Y.AsInt * mapData.Settings.MapSize.X.AsInt;

        if (!pelletGOs.TryGetValue(index, out GameObject pellet)) {
            return;
        }

        Destroy(pellet);
        pelletGOs.Remove(index);
    }

    public unsafe void OnEventPelletRespawn(EventPelletRespawn e) {
        DestroyPellets();

        var frame = e.Game.Frames.Verified;
        QDictionary<FPVector2, byte> pellets = frame.ResolveDictionary(frame.Global->PelletData);

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
            newPellet.transform.position = FPVectorUtils.CellToWorld(cell, frame).XOY.ToUnityVector3();
            pelletGOs.Add(FPVectorUtils.CellToIndex(cell, frame), newPellet);
        }
    }

    public void OnEventPowerPelletEat(EventPowerPelletEat e) {
        audioSource.PlayOneShot(powerPelletClip);
    }

    private void DestroyPellets() {
        foreach ((var _, var pellet) in pelletGOs) {
            Destroy(pellet);
        }
        pelletGOs.Clear();
    }
}