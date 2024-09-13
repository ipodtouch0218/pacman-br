using Quantum;
using UnityEngine;

public class MazeHandler : MonoBehaviour {

    public void Start() {
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
    }

    public void OnGameStarting(EventGameStarting e) {
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(i == e.MazeIndex);
        }
    }
}
