using Quantum;
using Quantum.Pacman;

public class PlayerReady : QuantumSceneViewComponent {
    public override void OnActivate(Frame f) {
        foreach (int slot in Game.GetLocalPlayerSlots()) {
            Game.SendCommand(slot, new CommandPlayerReady());
        }
    }
}