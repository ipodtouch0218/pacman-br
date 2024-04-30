using Quantum;
using UnityEngine;

public class PointHandler : MonoBehaviour {

    [SerializeField] private PointIndicator prefab;
    [SerializeField] private Vector3 offset;

    public void Start() {
        QuantumEvent.Subscribe<EventCharacterEaten>(this, OnCharacterEaten);
    }

    public void OnCharacterEaten(EventCharacterEaten e) {
        if (!e.Frame.TryGet(e.Pacman, out Transform2D t)) {
            return;
        }

        PointIndicator indicator = Instantiate(prefab, t.Position.ToUnityVector3() + offset, prefab.transform.rotation);
        indicator.Initialize(e.Combo);
    }
}