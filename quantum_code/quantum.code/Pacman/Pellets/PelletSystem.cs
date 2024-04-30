using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum.Pacman.Pellets {
    public unsafe class PelletSystem : SystemMainThread, ISignalOnGridMoverChangeTile {

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
        }

        public void OnGridMoverChangeTile(Frame f, EntityRef entity, FPVector2 tile) {

            if (!f.Unsafe.TryGetPointer(entity, out PacmanPlayer* player)) {
                return;
            }

            QDictionary<FPVector2, byte> pelletDict = f.ResolveDictionary(f.Global->PelletData);

            if (!pelletDict.TryGetValue(tile, out byte value)) {
                return;
            }

            player->PelletsEaten++;
            if (value == 2) {
                player->HasPowerPellet = true;
                f.Signals.OnPowerPelletStart();

                f.Global->PowerPelletDuration = 10;
                f.Global->PowerPelletTotalDuration = 10;

                f.Events.PowerPelletEat(entity);
            }

            pelletDict.Remove(tile);

            f.Events.PelletEat(entity, tile);

            // TODO: follow a pattern? random?
            if (pelletDict.Count <= 0) {
                var map = f.FindAsset<MapCustomData>(f.Map.UserAsset);
                int designs = map.PelletData.Length / (map.MapSize.X.AsInt * map.MapSize.Y.AsInt);

                SpawnNewPellets(f, f.Global->RngSession.Next(0, designs));
            }
        }
    }
}
