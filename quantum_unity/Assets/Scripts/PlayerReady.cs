using Quantum;
using Quantum.Platformer;

public class PlayerReady : QuantumCallbacks {

    public override void OnGameStart(QuantumGame game) {
        foreach (int player in game.GetLocalPlayers()) {
            game.SendCommand(player, new PlayerReadyCommand());
        }
    }
}