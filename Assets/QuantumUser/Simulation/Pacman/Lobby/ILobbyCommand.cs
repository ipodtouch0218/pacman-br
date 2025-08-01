using Quantum;

public unsafe interface ILobbyCommand {
    public void Execute(Frame f, PlayerRef sender, PlayerData* playerData);
}