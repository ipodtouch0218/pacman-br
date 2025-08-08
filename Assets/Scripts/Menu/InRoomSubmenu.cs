using Quantum;
using Quantum.Pacman;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

public class InRoomSubmenu : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private PlayerListEntry template;
    [SerializeField] private GameObject mainMenuPanel;

    //---Private Variables
    private readonly List<PlayerListEntry> entries = new();

    public void Start() {
        QuantumEvent.Subscribe<EventPlayerJoined>(this, OnPlayerJoined);
        QuantumEvent.Subscribe<EventPlayerLeft>(this, OnPlayerLeft);
    }

    public unsafe void OnEnable() {
        var runner = NetworkHandler.Instance.Runner;
        if (runner == null) {
            return;
        }
        
        Frame f = runner.Game.Frames.Predicted;
        foreach (var (_,playerData) in f.Unsafe.GetComponentBlockIterator<PlayerData>()) {
            var newEntry = Instantiate(template, template.transform.parent);
            newEntry.Initialize(f, playerData->PlayerRef);
            entries.Add(newEntry);
        }
    }

    public void OnDisable() {
        foreach (var entry in entries) {
            Destroy(entry);
        }
        entries.Clear();
    }

    [Preserve]
    public void QuitButton_Click() {
        NetworkHandler.Instance.Runner.Shutdown();
        gameObject.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    [Preserve]
    public void ReadyButton_Click() {
        var runner = NetworkHandler.Instance.Runner;
        var game = runner.Game;

        foreach (var slot in game.GetLocalPlayerSlots()) {
            game.SendCommand(slot, new CommandPlayerReady());
        }
    }

    private void OnPlayerJoined(EventPlayerJoined e) {
        var newEntry = Instantiate(template, template.transform.parent);
        newEntry.Initialize(e.Game.Frames.Verified, e.Player);
        entries.Add(newEntry);
    }

    private void OnPlayerLeft(EventPlayerLeft e) {
        foreach (var entry in entries) {
            if (entry.Player == e.Player) {
                Destroy(entry);
            }
        }
        entries.RemoveAll(entry => entry.Player == e.Player);
    }
}
