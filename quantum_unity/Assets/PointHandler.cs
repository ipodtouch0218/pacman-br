using Quantum;
using Quantum.Util;
using UnityEngine;

public class PointHandler : MonoBehaviour {

    [SerializeField] private PointIndicator prefab;
    [SerializeField] private PelletPointIndicator pelletPrefab;
    [SerializeField] private Vector3 offset;

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
        PelletPointIndicator indicator = Instantiate(pelletPrefab, FPVectorUtils.CellToWorld(e.Tile, e.Frame).ToUnityVector3(), prefab.transform.rotation);
        indicator.Initialize(e.Chain);
    }
}