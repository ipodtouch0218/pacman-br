using Photon.Deterministic;
using Quantum;
using Quantum.Util;
using System.Collections.Generic;
using UnityEngine;

public class PointHandler : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private PointIndicator ghostPrefab;
    [SerializeField] private PointIndicator fruitPrefab;
    [SerializeField] private PelletPointIndicator pelletPrefab;
    [SerializeField] private Vector3 offset;

    [SerializeField] private Color[] colors;

    //---Private Variables
    private readonly Dictionary<FPVector2, GameObject> pelletIndicators = new();

    public void Start() {
        QuantumEvent.Subscribe<EventCharacterEaten>(this, OnCharacterEaten);
        QuantumEvent.Subscribe<EventPelletEat>(this, OnPelletEat);
        QuantumEvent.Subscribe<EventFruitEaten>(this, OnFruitEaten);
    }

    public void OnCharacterEaten(EventCharacterEaten e) {
        if (!e.Frame.TryGet(e.Pacman, out Transform2D t)) {
            return;
        }

        PointIndicator indicator = Instantiate(ghostPrefab, t.Position.ToUnityVector3() + offset, ghostPrefab.transform.rotation);
        indicator.Initialize(e.GainedPoints);
    }

    public void OnPelletEat(EventPelletEat e) {
        if (pelletIndicators.TryGetValue(e.Tile, out GameObject obj) && obj) {
            Destroy(obj);
        }

        PelletPointIndicator indicator = Instantiate(pelletPrefab, FPVectorUtils.CellToWorld(e.Tile, e.Frame).ToUnityVector3(), pelletPrefab.transform.rotation);
        indicator.Initialize(e.Chain, colors[Mathf.Clamp(e.Chain / 10, 0, colors.Length - 1)]);
        pelletIndicators[e.Tile] = indicator.gameObject;
    }

    public void OnFruitEaten(EventFruitEaten e) {
        if (!e.Game.Frames.Verified.TryGet(e.Pacman, out Transform2D t)) {
            return;
        }

        PointIndicator indicator = Instantiate(fruitPrefab, t.Position.ToUnityVector3() + offset, fruitPrefab.transform.rotation);
        indicator.Initialize(e.Points);
    }
}