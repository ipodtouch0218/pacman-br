using Quantum;
using UnityEngine;

public unsafe class MainMenuStateHandler : MonoBehaviour {

    public void Start() {
        QuantumEvent.Subscribe<EventGameStateChanged>(this, OnGameStateChanged);
        QuantumCallback.Subscribe<CallbackGameStarted>(this, OnGameStarted);
        QuantumCallback.Subscribe<CallbackGameDestroyed>(this, OnGameDestroyed);
    }

    public void HandleState(GameState? state) {
        gameObject.SetActive(state == null || state == GameState.PreGameLobby);
    }

    private void OnGameStateChanged(EventGameStateChanged e) {
        HandleState(e.Game.Frames.Predicted.Global->GameState);
    }

    private void OnGameStarted(CallbackGameStarted e) {
        HandleState(e.Game.Frames.Predicted.Global->GameState);
    }

    private void OnGameDestroyed(CallbackGameDestroyed e) {
        HandleState(null);
    }
}