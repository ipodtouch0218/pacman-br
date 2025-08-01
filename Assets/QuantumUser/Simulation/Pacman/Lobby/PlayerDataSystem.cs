namespace Quantum {
    public unsafe class PlayerDataSystem : SystemSignalsOnly, ISignalOnPlayerAdded, ISignalOnPlayerRemoved, ISignalOnPlayerReady {

        public void OnPlayerReady(Frame f, PlayerRef player) {
            var filter = f.Filter<PlayerData>();
            while (filter.NextUnsafe(out _, out PlayerData* playerData)) {
                if (playerData->IsSpectator) {
                    continue;
                }

                if (!playerData->IsReady) {
                    return;
                }
            }

            f.Signals.OnAllPlayersReady();
        }

        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime) {
            var dict = f.ResolveDictionary(f.Global->PlayerDatas);
            EntityRef newEntity = f.Create();
            var newPlayerData = new PlayerData {
                PlayerRef = player,
                JoinTick = f.Number,
                IsRoomHost = dict.Count == 0,
            };
            f.Add(newEntity, newPlayerData);
            dict[player] = newEntity;
        }

        public void OnPlayerRemoved(Frame f, PlayerRef player) {
            var dict = f.ResolveDictionary(f.Global->PlayerDatas);
            if (!dict.TryGetValue(player, out var entity)) {
                return;
            }
            
            var playerData = f.Unsafe.GetPointer<PlayerData>(entity);

            if (playerData->IsRoomHost) {
                // Give room host to the next oldest player.
                var filter = f.Filter<PlayerData>();
                PlayerData* youngest = null;
                while (filter.NextUnsafe(out var entity2, out var playerData2)) {
                    if (entity == entity2) {
                        continue;
                    }
                    if (youngest == null || playerData2->JoinTick < youngest->JoinTick) {
                        youngest = playerData2;
                    }
                }

                if (youngest != null) {
                    youngest->IsRoomHost = true;
                }
            }
                
            f.Destroy(entity);
            dict.Remove(player);
        }
    }
}