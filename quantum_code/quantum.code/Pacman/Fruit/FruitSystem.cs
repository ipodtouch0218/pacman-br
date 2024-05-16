using Photon.Deterministic;
using Quantum.Pacman.Pellets;

namespace Quantum.Pacman.Fruit {
    public unsafe class FruitSystem : SystemSignalsOnly, ISignalOnTrigger2D, ISignalOnPelletRespawn {

        public static void SpawnFruit(Frame f) {

            if (f.Exists(f.Global->CurrentFruit)) {
                // Don't spawn
                return;
            }

            var map = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);

            FPVector2 spawnpoint = map.GhostHouse + FPVector2.Down * 3; // Default to below the ghost house

            // Find the spawnpoint with the highest minimum distance
            FP spawnpointDistance = 0;

            foreach (FPVector2 possibleSpawn in map.FruitSpawnPoints) {
                FP minimumDistance = FP.UseableMax;

                var players = f.Filter<Transform2D, PacmanPlayer>();
                while (players.Next(out _, out Transform2D playerTransform, out _)) {
                    FP distance = FPVector2.DistanceSquared(possibleSpawn, playerTransform.Position);
                    if (distance < minimumDistance) {
                        minimumDistance = distance;
                    }
                }

                if (spawnpointDistance < minimumDistance) {
                    spawnpointDistance = minimumDistance;
                    spawnpoint = possibleSpawn;
                }
            }

            var fruitPrototype = f.FindAsset<EntityPrototype>(map.FruitPrototype.Id);
            EntityRef newFruit = f.Create(fruitPrototype);
            f.Global->CurrentFruit = newFruit;

            if (f.Unsafe.TryGetPointer(newFruit, out Transform2D* transform) && f.Unsafe.TryGetPointer(newFruit, out Quantum.Fruit* fruit)) {
                transform->Position = spawnpoint;

                var fruitData = map.FruitSpawnOrder[FPMath.Clamp(f.Global->FruitsSpawned, 0, map.FruitSpawnOrder.Length - 1)];
                fruit->Graphic = fruitData.SpriteIndex;
                fruit->Points = fruitData.Points;
            }
            f.Global->FruitsSpawned++;
        }

        public void OnPelletRespawn(Frame f) {
            var fruit = f.Filter<Quantum.Fruit>();
            while (fruit.Next(out EntityRef entity, out _)) {
                f.Destroy(entity);
            }
        }

        public void OnTrigger2D(Frame f, TriggerInfo2D info) {

            if (!f.TryGet(info.Entity, out PacmanPlayer pac) || pac.IsDead) {
                return;
            }

            if (!f.TryGet(info.Other, out Quantum.Fruit fruit) || f.DestroyPending(info.Other) || !f.TryGet(info.Other, out Transform2D transform)) {
                return;
            }

            // Pacman interacting with a fruit.

            f.Events.FruitEaten(info.Entity, info.Other);
            f.Signals.OnPacmanScored(info.Entity, fruit.Points);
            f.Destroy(info.Other);

            var map = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);
            PelletSystem.SpawnNextPellets(f, transform.Position.X < map.GhostHouse.X);
        }
    }
}
