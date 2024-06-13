using Quantum.Pacman;
using Quantum.Pacman.Fruit;
using Quantum.Pacman.Ghost;
using Quantum.Pacman.Logic;
using Quantum.Pacman.Pellets;
using Quantum.Platformer;

namespace Quantum {
    public static class SystemSetup {
        public static SystemBase[] CreateSystems(RuntimeConfig gameConfig, SimulationConfig simulationConfig) {
            return new SystemBase[] {
                // pre-defined core systems
                //new Core.CullingSystem2D(),
                //new Core.CullingSystem3D(),

                new Core.PhysicsSystem2D(),
                //new Core.PhysicsSystem3D(),

                Core.DebugCommand.CreateSystem(),

                //new Core.NavigationSystem(),
                new Core.EntityPrototypeSystem(),
                new Core.PlayerConnectedSystem(),

                // user systems go here
                new GameLogicSystem(),
                new PausableSystemGroup("Freezable Systems", new SystemBase[] {
                    new GhostAISystem(),
                    new GridMovementSystem(),
                    new PelletSystem(),
                    new PacmanSystem(),
                }),
                new GhostHouseSystem(),
                new FruitSystem(),
                new PlayerSpawnSystem(),
            };
        }
    }
}
