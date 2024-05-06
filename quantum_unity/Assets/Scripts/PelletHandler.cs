using Quantum;
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
    private readonly Dictionary<int, GameObject> pellets = new();

    public void OnValidate() {
        if (!audioSource) {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void Awake() {
        QuantumEvent.Subscribe<EventPelletRespawn>(this, OnEventPelletRespawn);
        QuantumEvent.Subscribe<EventPelletEat>(this, OnEventPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnEventPowerPelletEat);

        OnEventPelletRespawn(new EventPelletRespawn() {
            Configuration = 0
        });
    }

    public void OnEventPelletEat(EventPelletEat e) {
        int index = e.Tile.X.AsInt + e.Tile.Y.AsInt * mapData.Settings.MapSize.X.AsInt;

        if (!pellets.TryGetValue(index, out GameObject pellet)) {
            return;
        }

        Destroy(pellet);
        pellets.Remove(index);
    }

    public void OnEventPelletRespawn(EventPelletRespawn e) {
        DestroyPellets();

        int size = mapData.Settings.MapSize.X.AsInt * mapData.Settings.MapSize.Y.AsInt;
        int offset = e.Configuration * size;

        for (int x = 0; x < mapData.Settings.MapSize.X; x++) {
            for (int y = 0; y < mapData.Settings.MapSize.Y; y++) {
                int index = (x + y * mapData.Settings.MapSize.X.AsInt) + offset;

                GameObject prefab = mapData.Settings.PelletData[index] switch {
                    1 => smallPelletPrefab,
                    2 => powerPelletPrefab,
                    _ => null
                };

                if (prefab == null) {
                    continue;
                }

                GameObject newPellet = Instantiate(prefab, transform, true);
                newPellet.transform.position = new Vector3(x, 0, y) + mapData.Settings.MapOrigin.XOY.ToUnityVector3();
                pellets.Add(index - offset, newPellet);
            }
        }
    }

    public void OnEventPowerPelletEat(EventPowerPelletEat e) {
        audioSource.PlayOneShot(powerPelletClip);
    }

    private void DestroyPellets() {
        foreach ((var _, var pellet) in pellets) {
            Destroy(pellet);
        }
        pellets.Clear();
    }
}