using Quantum.Pacman.Fruits;
using Quantum.Pacman.Ghosts;
using Quantum.Pacman.Logic;
using Quantum.Pacman.Pellets;
using System.Collections.Generic;

namespace Quantum
{
    public static partial class DeterministicSystemSetup
    {
        static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
        {
            systems.Clear();
            systems.Add(Core.DebugCommand.CreateSystem());
            systems.Add(new Core.EntityPrototypeSystem());
            systems.Add(new Core.PlayerConnectedSystem());
            
            /// 

            systems.Add(new PlayerDataSystem());
            systems.Add(new GameLogicSystem());
            systems.Add(new GameplaySystemGroup(
                new Core.PhysicsSystem2D(),
                new GhostAISystem(),
                new GridMovementSystem(),
                new PelletSystem(),
                new PacmanSystem(),
                new GhostHouseSystem(),
                new FruitSystem()
            ));
        }
    }
}