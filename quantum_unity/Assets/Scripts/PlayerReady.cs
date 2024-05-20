using Quantum.Platformer;
using System;
using System.Collections;
using UnityEngine;

public class PlayerReady : QuantumCallbacks {

    public void Start() {
        var game = QuantumRunner.Default.Game;
        StartCoroutine(Wait(() => {
            foreach (int player in game.GetLocalPlayers()) {
                game.SendCommand(player, new PlayerReadyCommand());
            }
        }, 1));
    }

    private IEnumerator Wait(Action action, float time) {
        yield return new WaitForSeconds(time);
        action();
    }
}