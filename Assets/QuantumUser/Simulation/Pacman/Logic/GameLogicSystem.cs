using Photon.Deterministic;
using Quantum.Pacman.Pellets;
using System.Collections.Generic;

namespace Quantum.Pacman.Logic {
    public unsafe class GameLogicSystem : SystemMainThread, ISignalOnAllPlayersReady {

        public override void OnInit(Frame f) {
            f.SimulationConfig.DefaultRules.Materialize(f, ref f.Global->GameRules);

            if (f.RuntimeConfig.IsRealGame) {
                f.Global->GameState = GameState.PreGameLobby;
            } else {
                f.Global->GameState = GameState.WaitingForPlayers;
            }
        }

        public override void Update(Frame f) {
            // Execute commands
            var playerDataDict = f.ResolveDictionary(f.Global->PlayerDatas);
            for (int sender = 0; sender < f.PlayerCount; sender++) {
                if (playerDataDict.TryGetValue(sender, out EntityRef entity)
                    && f.Unsafe.TryGetPointer(entity, out PlayerData* playerData)) {
                    if (f.GetPlayerCommand(sender) is ILobbyCommand cmd) {
                        cmd.Execute(f, sender, playerData);
                    }
                }
            }

            switch (f.Global->GameState) {
            case GameState.WaitingForPlayers:
                if (f.Global->StateTimeoutTimer == 0) {
                    f.Global->StateTimeoutTimer = 30;
                }

                if ((f.Global->StateTimeoutTimer -= f.DeltaTime) <= 0) {
                    // Start game
                    StartGame(f);
                }
                break;
            case GameState.Starting:
                if (f.Global->StateTimeoutTimer == 0) {
                    f.Global->StateTimeoutTimer = 5;
                }

                if ((f.Global->StateTimeoutTimer -= f.DeltaTime) <= 0) {
                    // Start game
                    f.Global->StateTimeoutTimer = 0;
                    f.Global->GameStartTick = f.Number;
                    ChangeGameState(f, GameState.Playing);

                    f.SystemEnable<GameplaySystemGroup>();
                    f.Events.GameStart();
                }
                break;
            case GameState.Playing:
                FP previousTimer = f.Global->Timer;
                FP newTimer = f.Global->Timer -= f.DeltaTime;
                if (newTimer <= 0) {
                    // Game end!
                    ChangeGameState(f, GameState.Scoreboard);
                    f.Global->Timer = 0;

                    var filter = f.Filter<PacmanPlayer>();
                    while (filter.NextUnsafe(out _, out var pac)) {
                        int bombScore = pac->Bombs * (pac->Bombs + 1) * 750 / 2; // equal to sum(750x)
                        pac->RoundScore += bombScore;
                    }

                    f.SystemDisable<GameplaySystemGroup>();
                    f.Events.GameEnd();
                } else {
                    // Mid-game
                    var maze = PacmanStageMapData.Current(f).CurrentMazeData(f);

                    if (FPMath.CeilToInt(newTimer) != FPMath.CeilToInt(previousTimer)) {
                        // A second passed!
                        f.Events.TimerSecondPassed(FPMath.CeilToInt(newTimer));
                    }

                    FP increasePercentage = maze.LevelCurve.Evaluate(newTimer / f.Global->GameRules.TimerSeconds);
                    int newLevel = FPMath.FloorToInt(f.Global->GameRules.MinLevel + f.Global->GameRules.LevelRange * increasePercentage);
                    SetGameSpeedLevel(f, newLevel);
                }
                break;
            case GameState.Scoreboard:
                if (f.Global->StateTimeoutTimer == 0) {
                    f.Global->StateTimeoutTimer = 30;
                }

                if ((f.Global->StateTimeoutTimer -= f.DeltaTime) <= 0) {
                    // Start game
                    ProgressFromScoreboard(f);
                }
                break;
            }
        }

        public void OnAllPlayersReady(Frame f) {
            switch (f.Global->GameState) {
            case GameState.PreGameLobby:
                if (f.IsVerified) {
                    f.MapAssetRef = f.Global->GameRules.Map;
                }
                ChangeGameState(f, GameState.WaitingForPlayers);
                break;
            case GameState.WaitingForPlayers or GameState.Scoreboard:
                ProgressFromScoreboard(f);
                break;
            }
        }

        public static void ChangeGameState(Frame f, GameState newState) {
            GameState oldState = f.Global->GameState;
            f.Global->GameState = newState;
            f.Events.GameStateChanged(oldState, newState);
        }

        public static void ProgressFromScoreboard(Frame f) {
            if (f.Global->CurrentMazeIndex + 1 >= PacmanStageMapData.Current(f).Mazes.Length) {
                ReturnToPreGameLobby(f);
            } else {
                StartGame(f);
            }
        }

        public static void ReturnToPreGameLobby(Frame f) {
            List<EntityRef> allEntities = new();
            f.GetAllEntityRefs(allEntities);
            foreach (var entity in allEntities) {
                if (!f.Has<PlayerData>(entity)) {
                    f.Destroy(entity);
                }
            }
            ChangeGameState(f, GameState.PreGameLobby);
        }

        public static void SpawnPacman(Frame f, PlayerRef player, int ranking) {
            var entity = f.Create(PacmanStageMapData.Current(f).PacmanPrototype);

            var playerlink = new PlayerLink() {
                Player = player
            };
            f.Add(entity, playerlink);

            if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pac)) {
                pac->RoundRanking = pac->PreviousRoundRanking = new Ranking() {
                    SharedRanking = 0,
                    UniqueRanking = (byte) ranking,
                };
            }
        }

        public static void ChangeRound(Frame f, int newRound) {

            f.Global->CurrentMazeIndex = newRound;
            PacmanStageMapData.MazeData maze = PacmanStageMapData.Current(f).CurrentMazeData(f);

            // Remove fruit
            var fruitFilter = f.Filter<Fruit>();
            while (fruitFilter.NextUnsafe(out EntityRef entity, out _)) {
                f.Destroy(entity);
            }

            // Reset movers
            foreach (var (en,mover) in f.Unsafe.GetComponentBlockIterator<GridMover>()) { 
                mover->SpeedMultiplier = 1;
                mover->TeleportFrames = 0;
                mover->IsLocked = false;
                mover->IsStationary = false;
                mover->DistanceMoved = 0;
                mover->FreezeTime = 0;
            }

            // Reset players
            var pacFilter = f.Filter<PacmanPlayer, GridMover, Transform2D, PlayerLink>();
            while (pacFilter.NextUnsafe(out _, out var pac, out var mover, out var transform, out var pl)) {
                // Reset
                pac->Reset();

                // Move to new maze
                var spawnpoint = maze.SpawnPoints[pl->Player % maze.SpawnPoints.Length];
                transform->Position = spawnpoint.Position;
                mover->Direction = spawnpoint.Direction;

                // Scoring
                pac->TotalScore += pac->RoundScore;
                pac->RoundScore = 0;

                pac->PreviousRoundRanking = pac->RoundRanking;
                pac->RoundRanking = new Ranking() {
                    SharedRanking = 0,
                    UniqueRanking = (byte) pl->Player,
                };
            }

            // Reset ghosts
            var ghostFilter = f.Filter<Ghost, Transform2D, GridMover>();
            while (ghostFilter.NextUnsafe(out _, out var ghost, out var transform, out var mover)) {

                ghost->State = GhostState.Scatter;
                ghost->TimeSinceEaten = 0;
                ghost->ForceRandomMovement = false;
                ghost->ScatterTimer = 5;

                FPVector2 offset = FPVector2.Zero;
                FPVector2 targetOffset = FPVector2.Zero;
                switch (ghost->Mode) {
                case GhostTargetMode.Blinky:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 10;
                    offset = FPVector2.Up;
                    mover->Direction = 3;
                    targetOffset = FPVector2.Down;
                    break;
                case GhostTargetMode.Pinky:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 15;
                    offset = FPVector2.Down;
                    mover->Direction = 1;
                    targetOffset = FPVector2.Up;
                    break;
                case GhostTargetMode.Inky:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 20;
                    offset = FPVector2.Left * 2;
                    mover->Direction = 3;
                    targetOffset = FPVector2.Down;
                    break;
                case GhostTargetMode.Clyde:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 30;
                    offset = FPVector2.Right * 2;
                    mover->Direction = 3;
                    targetOffset = FPVector2.Down;
                    break;
                }

                transform->Position = maze.GhostHouse + offset;
                if (ghost->GhostHouseState == GhostHouseState.Waiting) {
                    ghost->TargetPosition = transform->Position + targetOffset;
                }

                f.ResolveList(f.Global->GhostHouseQueue).Clear();
            }

            f.Global->CurrentLayout = 0;

            // Reset pellets
            f.Global->PowerPelletRemainingTime = 0;
            PelletSystem.SpawnNewPellets(f, 0, false, false);
        }

        public static void StartGame(Frame f) {
            if (f.Global->GameState == GameState.WaitingForPlayers) {
                int i = 0;
                foreach (var (_, playerData) in f.Unsafe.GetComponentBlockIterator<PlayerData>()) {
                    playerData->IsSpectator = !playerData->IsReady;

                    if (!playerData->IsSpectator) {
                        SpawnPacman(f, playerData->PlayerRef, i++);
                    }
                    playerData->IsReady = false;
                }
            }

            int index = 0;
            if (f.Global->GameState != GameState.Scoreboard) {
                f.Global->CurrentMazeIndex = -1;
            }
            index = ++f.Global->CurrentMazeIndex;

            ChangeRound(f, index);
            f.Global->GameState = GameState.Starting;
            f.Global->StateTimeoutTimer = 5;
            f.Global->GameStartTick = 0;
            f.Global->FruitsSpawned = 0;
            f.Global->Timer = f.Global->GameRules.TimerSeconds;
            //f.Global->GhostsInScatterMode = true;
            SetGameSpeedLevel(f, f.Global->GameRules.MinLevel);
            f.Events.GameStarting(index);
        }

        public static void SetGameSpeedLevel(Frame f, int level) {
            level = FPMath.Clamp(level, 0, 50);

            FP speed;
            if (level < 10) {
                speed = FP.FromString("0.6356") * level + FP.FromString("3.214");
            } else {
                speed = FP.FromString("0.08575") * level + FP.FromString("8.7125");
            }

            f.Global->GameSpeed = speed;
        }
    }
}
