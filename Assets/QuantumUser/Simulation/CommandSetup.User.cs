using System.Collections.Generic;
using Photon.Deterministic;
using Quantum.Pacman;

namespace Quantum {
    public static partial class DeterministicCommandSetup {
        static partial void AddCommandFactoriesUser(ICollection<IDeterministicCommandFactory> factories, RuntimeConfig gameConfig, SimulationConfig simulationConfig) {
            // user commands go here

            factories.Add(new PlayerReadyCommand());
            factories.Add(new StartNextRoundCommand());
        }
    }
}
