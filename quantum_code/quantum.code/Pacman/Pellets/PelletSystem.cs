using Photon.Deterministic;
using Quantum.Collections;
using Quantum.Pacman.Fruit;
using Quantum.Util;

namespace Quantum.Pacman.Pellets {
    public unsafe class PelletSystem : SystemMainThread, ISignalOnPowerPelletEnd, ISignalOnPacmanRespawned, ISignalOnGridMoverChangeTile {

        public override void OnInit(Frame f) {
            f.Global->PelletData = f.AllocateDictionary<FPVector2, byte>();

            SpawnNewPellets(f, 0, false, false);
        }

        public override void Update(Frame f) {
            FP remainingPowerPelletTime = 0;

            var filtered = f.Filter<PacmanPlayer>();
            while (filtered.NextUnsafe(out EntityRef entity, out PacmanPlayer* player)) {
                if (player->PowerPelletTimer > 0) {
                    if ((player->PowerPelletTimer -= f.DeltaTime) <= 0) {
                        player->PowerPelletTimer = 0;
                        f.Signals.OnPowerPelletEnd(entity);
                        f.Events.PowerPelletEnd(entity);
                    }
                }

                remainingPowerPelletTime = FPMath.Max(remainingPowerPelletTime, player->PowerPelletTimer);
            }

            f.Global->PowerPelletRemainingTime = remainingPowerPelletTime;
        }

        public static void SpawnNextPellets(Frame f, bool fromLeft) {
            SpawnNewPellets(f, ++f.Global->CurrentLayout, true, fromLeft);
        }

        public static void SpawnNewPellets(Frame f, int pelletConfig, bool playEffect, bool fromLeft) {
            QDictionary<FPVector2, byte> pelletDict = f.ResolveDictionary(f.Global->PelletData);
            pelletDict.Clear();

            var map = f.FindAsset<MapCustomData>(f.Map.UserAsset);
            var pellets = map.PelletData;

            int designs = map.PelletData.Length / (map.MapSize.X.AsInt * map.MapSize.Y.AsInt);
            pelletConfig = FPMath.Repeat(pelletConfig, designs - 1).AsInt;


            int offset = pelletConfig * map.MapSize.X.AsInt * map.MapSize.Y.AsInt;
            int count = 0;
            for (int x = 0; x < map.MapSize.X; x++) {
                for (int y = 0; y < map.MapSize.Y; y++) {
                    int index = x + (y * map.MapSize.X.AsInt) + offset;
                    byte pellet = pellets[index];
                    if (pellet != 0) {
                        pelletDict.Add(new FPVector2(x, y), pellet);
                        count++;
                    }
                }
            }
            f.Global->TotalPellets = count;

            f.Signals.OnPelletRespawn();
            f.Events.PelletRespawn(pelletConfig, playEffect, fromLeft);

            var filter = f.Filter<PacmanPlayer, Transform2D>();
            while (filter.Next(out EntityRef entity, out PacmanPlayer pac, out Transform2D transform)) {
                if (pac.IsDead) {
                    continue;
                }

                FPVector2 tile = FPVectorUtils.WorldToCell(transform.Position, f);
                TryEatPellet(f, entity, tile, false);
            }
        }

        public void OnGridMoverChangeTile(Frame f, EntityRef entity, FPVector2 tile) {
            TryEatPellet(f, entity, tile, true);
        }

        public void OnPacmanRespawned(Frame f, EntityRef entity) {
            if (!f.TryGet(entity, out Transform2D transform)) {
                return;
            }

            if (!f.Unsafe.TryGetPointer(entity, out PacmanPlayer* player)) {
                return;
            }

            player->PelletChain = 0;
            TryEatPellet(f, entity, FPVectorUtils.WorldToCell(transform.Position, f), true);
        }

        private static void TryEatPellet(Frame f, EntityRef entity, FPVector2 tile, bool breakChain) {

            var map = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);
            if (tile.X < 0 || tile.X >= map.MapSize.X) {
                return;
            }
            if (tile.Y < 0 || tile.Y >= map.MapSize.Y) {
                return;
            }

            if (!f.Unsafe.TryGetPointer(entity, out PacmanPlayer* player)
                || !f.Unsafe.TryGetPointer(entity, out GridMover* mover)
                || !f.TryGet(entity, out Transform2D transform)) {

                return;
            }

            QDictionary<FPVector2, byte> pelletDict = f.ResolveDictionary(f.Global->PelletData);

            if (!pelletDict.TryGetValue(tile, out byte value)) {
                if (breakChain) {
                    player->PelletChain = 0;
                }
                return;
            }

            player->PelletChain++;
            player->PelletsEaten++;
            // mover->FreezeTime += f.DeltaTime;
            if (value == 2) {
                f.Signals.OnPowerPelletStart(entity);
                player->PowerPelletTimer = FP.FromString("7.5");
                mover->SpeedMultiplier = FP._1;

                f.Events.PowerPelletEat(entity);
            }

            pelletDict.Remove(tile);

            f.Signals.OnPacmanScored(entity, player->PelletChain);
            f.Events.PelletEat(f, entity, tile, player->PelletChain);

            if (pelletDict.Count == (f.Global->TotalPellets * FP._0_33).AsInt) {
                FruitSystem.SpawnFruit(f);
            }

            /*
            // TODO: follow a pattern? random?
            if (pelletDict.Count <= 0) {
                SpawnNextPellets(f, transform.Position.X < map.GhostHouse.X);
            }
            */
        }

        public void OnPowerPelletEnd(Frame f, EntityRef pacman) {

            bool playerHasPowerPellet = false;
            var filtererdPacman = f.Filter<PacmanPlayer>();
            while (filtererdPacman.Next(out _, out PacmanPlayer otherPacman)) {
                playerHasPowerPellet |= otherPacman.HasPowerPellet;
            }

            if (!playerHasPowerPellet) {
                // No longer power pellet
                var filteredGhosts = f.Filter<GridMover, Quantum.Ghost>();
                while (filteredGhosts.NextUnsafe(out EntityRef entity, out GridMover* mover, out Quantum.Ghost* ghost)) {
                    if (ghost->State == GhostState.Scared) {
                        ghost->ChangeState(f, entity, GhostState.Chase);
                    }
                }

                var filteredPacman2 = f.Filter<PacmanPlayer, GridMover>();
                while (filteredPacman2.NextUnsafe(out _, out _, out GridMover* mover)) {
                    mover->SpeedMultiplier = FP._1;
                }
            }
        }
    }
}
