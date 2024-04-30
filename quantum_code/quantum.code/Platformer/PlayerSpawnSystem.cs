namespace Quantum.Platformer {
    public unsafe class PlayerSpawnSystem : SystemSignalsOnly, ISignalOnPlayerDataSet {
        public void OnPlayerDataSet(Frame f, PlayerRef player) {

            var data = f.GetPlayerData(player);

            var prototype = f.FindAsset<EntityPrototype>(data.CharacterPrototype.Id);

            var entity = f.Create(prototype);

            var playerlink = new PlayerLink() {
                Player = player
            };
            f.Add(entity, playerlink);

            if (f.Unsafe.TryGetPointer(entity, out GridMover* grid)) {
                if (f.Unsafe.TryGetPointer(entity, out Transform2D* transform)) {
                    var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset);
                    var spawnpoint = mapdata.SpawnPoints[(int) player % mapdata.SpawnPoints.Length];

                    transform->Position = spawnpoint.Position;
                    grid->Direction = spawnpoint.Direction;
                }
            }
        }
    }
}
