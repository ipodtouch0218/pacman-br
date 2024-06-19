using Photon.Deterministic;
using Quantum.Pacman.Pellets;

namespace Quantum.Pacman.Logic {
    public unsafe class GameLogicSystem : SystemMainThread, ISignalOnPlayerReady {

        public override void OnInit(Frame f) {
            // Temporary...
            f.Global->Timer = 180;

            for (int i = 0; i < f.PlayerCount; i++) {
                SpawnPacman(f, i);
            }
        }

        public override void Update(Frame f) {

            if (f.Global->GameStartingTimer > 0) {
                // Start game!
                if ((f.Global->GameStartingTimer -= f.DeltaTime) <= 0) {
                    f.Global->GameStarted = true;
                    f.Global->GameStartTick = f.Number;

                    f.SystemEnable<PausableSystemGroup>();
                    f.Events.GameStart();
                }
            } else if (f.Global->GameStarted) {
                FP previousTimer = f.Global->Timer;
                FP newTimer = f.Global->Timer -= f.DeltaTime;
                if (newTimer <= 0) {
                    // Game end!
                    f.Global->GameStarted = false;
                    f.Global->Timer = 0;

                    f.SystemDisable<PausableSystemGroup>();
                    f.Events.GameEnd();

                } else if (FPMath.CeilToInt(newTimer) != FPMath.CeilToInt(previousTimer)) {
                    // A second passed!
                    f.Events.TimerSecondPassed(FPMath.CeilToInt(newTimer));
                }
            } else {
                // Host started
                if (f.GetPlayerCommand(0) is StartNextRoundCommand) {
                    StartRound(f);
                }

                // Ready up
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
        }

        public static void SpawnPacman(Frame f, PlayerRef player) {

            var entity = f.Create(MapCustomData.Current(f).PacmanPrototype);

            var playerlink = new PlayerLink() {
                Player = player
            };
            f.Add(entity, playerlink);

            if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pac)) {
                pac->RoundRanking = pac->PreviousRoundRanking = new Ranking() {
                    SharedRanking = 0,
                    UniqueRanking = (byte) player,
                };
            }
        }

        public static void ChangeRound(Frame f, int newRound) {

            f.Global->CurrentMazeIndex = newRound;
            MapCustomData.MazeData maze = MapCustomData.Current(f).CurrentMazeData(f);

            // Reset pellets
            f.Global->CurrentLayout = 0;
            f.Global->PowerPelletRemainingTime = 0;
            PelletSystem.SpawnNewPellets(f, 0, false, false);

            // Remove fruit
            var fruitFilter = f.Filter<Quantum.Fruit>();
            while (fruitFilter.Next(out EntityRef entity, out _)) {
                f.Destroy(entity);
            }

            // Reset movers
            var moverFilter = f.Filter<GridMover>();
            while (moverFilter.NextUnsafe(out _, out GridMover* mover)) {
                mover->SpeedMultiplier = 1;
                mover->TeleportFrames = 0;
                mover->IsLocked = false;
                mover->IsStationary = false;
                mover->DistanceMoved = 0;
                mover->FreezeTime = 0;
            }

            // Reset players
            var pacFilter = f.Filter<PacmanPlayer, GridMover, Transform2D, PlayerLink>();
            while (pacFilter.NextUnsafe(out _, out PacmanPlayer* pac, out GridMover* mover, out Transform2D* transform, out PlayerLink* pl)) {
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
            var ghostFilter = f.Filter<Quantum.Ghost, Transform2D, GridMover>();
            while (ghostFilter.NextUnsafe(out _, out Quantum.Ghost* ghost, out Transform2D* transform, out GridMover* mover)) {

                ghost->State = GhostState.Chase;
                ghost->TimeSinceEaten = 0;
                ghost->ForceRandomMovement = false;

                FPVector2 offset = FPVector2.Zero;
                FPVector2 targetOffset = FPVector2.Zero;
                switch (ghost->Mode) {
                case GhostTargetMode.Blinky:
                    ghost->GhostHouseState = GhostHouseState.NotInGhostHouse;
                    ghost->GhostHouseWaitTime = 0;
                    mover->Direction = 0;
                    offset = FPVector2.Up * 3;
                    break;
                case GhostTargetMode.Pinky:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 10;
                    offset = FPVector2.Zero;
                    mover->Direction = 1;
                    targetOffset = FPVector2.Up;
                    break;
                case GhostTargetMode.Inky:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 15;
                    offset = FPVector2.Left * 2;
                    mover->Direction = 3;
                    targetOffset = FPVector2.Down;
                    break;
                case GhostTargetMode.Clyde:
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 20;
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
        }


        public void OnPlayerReady(Frame f, PlayerRef player) {
            var playerLinks = f.Filter<PlayerLink>();
            while (playerLinks.Next(out _, out PlayerLink playerLink)) {
                if (!playerLink.ReadyToPlay) {
                    return;
                }
            }

            // All players are ready!
            StartRound(f, 0);
        }

        public static void StartRound(Frame f, int? round = null) {
            int index = round.GetValueOrDefault(f.Global->CurrentMazeIndex + 1);
            ChangeRound(f, index);
            f.Global->GameStartingTimer = 5;
            f.Global->GameStarted = false;
            f.Global->GameStartTick = 0;
            f.Global->FruitsSpawned = 0;
            f.Global->Timer = 180;
            f.Events.GameStarting(index);
        }
    }
}
