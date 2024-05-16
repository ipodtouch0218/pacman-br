using Photon.Deterministic;
using Quantum;
using Quantum.Util;
using System.Collections.Generic;
using UnityEngine;

public class PointHandler : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private PointIndicator prefab;
    [SerializeField] private PelletPointIndicator pelletPrefab;
    [SerializeField] private Vector3 offset;

    [SerializeField] private Color[] colors;

    //---Private Variables
    private readonly Dictionary<FPVector2, GameObject> pelletIndicators = new();

    public void Start() {
        QuantumEvent.Subscribe<EventCharacterEaten>(this, OnCharacterEaten);
        QuantumEvent.Subscribe<EventPelletEat>(this, OnPelletEat);
    }

    public void OnCharacterEaten(EventCharacterEaten e) {
        if (!e.Frame.TryGet(e.Pacman, out Transform2D t)) {
            return;
        }

        PointIndicator indicator = Instantiate(prefab, t.Position.ToUnityVector3() + offset, prefab.transform.rotation);
        indicator.Initialize(e.Combo);
    }

    public void OnPelletEat(EventPelletEat e) {
        if (pelletIndicators.TryGetValue(e.Tile, out GameObject obj) && obj) {
            Destroy(obj);
        }

        PelletPointIndicator indicator = Instantiate(pelletPrefab, FPVectorUtils.CellToWorld(e.Tile, e.Frame).ToUnityVector3(), prefab.transform.rotation);
        indicator.Initialize(e.Chain, colors[Mathf.Clamp(e.Chain / 10, 0, colors.Length - 1)]);
        pelletIndicators[e.Tile] = indicator.gameObject;
    }
}