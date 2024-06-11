namespace Quantum.Platformer {
    public unsafe class PlayerSpawnSystem : SystemMainThread, ISignalOnPlayerDataSet {

        public override void Update(Frame f) {
            for (int i = 0; i < f.PlayerCount; i++) {
                if (f.GetPlayerCommand(i) is not PlayerReadyCommand) {
                    continue;
                }

                var playerLinks = f.Filter<PlayerLink>();
                while (playerLinks.NextUnsafe(out _, out PlayerLink* playerLink)) {
                    if (playerLink->Player != i || playerLink->ReadyToPlay) {
                        continue;
                    }

                    playerLink->ReadyToPlay = true;
                    f.Signals.OnPlayerReady(playerLink->Player);
                }
            }
        }

        public static void ClearAllPlayerReady(Frame f) {
            var filter = f.Filter<PlayerLink>();
            while (filter.NextUnsafe(out _, out PlayerLink* player)) {
                player->ReadyToPlay = false;
            }
        }

        public void OnPlayerDataSet(Frame f, PlayerRef player) {

            var data = f.GetPlayerData(player);

            var prototype = f.FindAsset<EntityPrototype>(data.CharacterPrototype.Id);

            var entity = f.Create(prototype);

            var playerlink = new PlayerLink() {
                Player = player
            };
            f.Add(entity, playerlink);

            if (!f.Unsafe.TryGetPointer(entity, out GridMover* grid) ||
                !f.Unsafe.TryGetPointer(entity, out Transform2D* transform)) {
                return;
            }

            MapCustomData.MazeData maze = MapCustomData.Current(f).CurrentMazeData(f);
            var spawnpoint = maze.SpawnPoints[player % maze.SpawnPoints.Length];

            transform->Position = spawnpoint.Position;
            grid->Direction = spawnpoint.Direction;
        }
    }
}
