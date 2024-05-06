using Photon.Deterministic;
using Quantum.Collections;
using Quantum.Util;

namespace Quantum.Pacman.Pellets {
    public unsafe class PelletSystem : SystemMainThread, ISignalOnPacmanRespawned, ISignalOnGridMoverChangeTile {

        public override void OnInit(Frame f) {
            f.Global->PelletData = f.AllocateDictionary<FPVector2, byte>();

            SpawnNewPellets(f, 0);
        }

        public override void Update(Frame f) {
            if (f.Global->PowerPelletDuration > 0) {
                if ((f.Global->PowerPelletDuration -= f.DeltaTime) <= 0) {
                    f.Global->PowerPelletDuration = 0;
                    f.Signals.OnPowerPelletEnd();
                    f.Events.PowerPelletEnd();
                }
            }
        }

        public static void SpawnNewPellets(Frame f, int pelletConfig) {
            QDictionary<FPVector2, byte> pelletDict = f.ResolveDictionary(f.Global->PelletData);
            pelletDict.Clear();

            var map = f.FindAsset<MapCustomData>(f.Map.UserAsset);
            var pellets = map.PelletData;

            int offset = pelletConfig * map.MapSize.X.AsInt * map.MapSize.Y.AsInt;
            for (int x = 0; x < map.MapSize.X; x++) {
                for (int y = 0; y < map.MapSize.Y; y++) {
                    int index = x + (y * map.MapSize.X.AsInt) + offset;
                    byte pellet = pellets[index];
                    if (pellet != 0) {
                        pelletDict.Add(new FPVector2(x, y), pellet);
                    }
                }
            }

            f.Events.PelletRespawn(pelletConfig);

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

            if (!f.Unsafe.TryGetPointer(entity, out PacmanPlayer* player) || !f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
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
            if (value == 2) {
                f.Signals.OnPowerPelletStart();
                player->HasPowerPellet = true;
                mover->SpeedMultiplier = FP._1;

                f.Global->PowerPelletDuration = 10;
                f.Global->PowerPelletTotalDuration = 10;

                f.Events.PowerPelletEat(entity);
            }

            pelletDict.Remove(tile);

            f.Signals.OnPacmanScored(entity, player->PelletChain);
            f.Events.PelletEat(f, entity, tile, player->PelletChain);

            // TODO: follow a pattern? random?
            if (pelletDict.Count <= 0) {
                var map = f.FindAsset<MapCustomData>(f.Map.UserAsset);
                int designs = map.PelletData.Length / (map.MapSize.X.AsInt * map.MapSize.Y.AsInt);

                SpawnNewPellets(f, f.Global->RngSession.Next(0, designs));
            }
        }
    }
}
