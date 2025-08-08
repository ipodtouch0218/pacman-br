using Photon.Deterministic;

namespace Quantum.Pacman {
    public unsafe class CommandPlayerReady : DeterministicCommand, ILobbyCommand {
        public override void Serialize(BitStream stream) {
            // Sorry, nothing.
        }

        public void Execute(Frame f, PlayerRef sender, PlayerData* playerData) {
            if (f.Global->GameState is GameState.PreGameLobby or GameState.WaitingForPlayers or GameState.Scoreboard) {
                if (!playerData->IsReady) {
                    playerData->IsReady = true;
                    f.Signals.OnPlayerReady(sender);
                }
            }
        }
    }
}
