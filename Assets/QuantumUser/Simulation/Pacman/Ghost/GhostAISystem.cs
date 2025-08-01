using Photon.Deterministic;
using Quantum.Physics2D;
using Quantum.Util;

namespace Quantum.Pacman.Ghosts {
    public unsafe class GhostAISystem : SystemMainThreadFilter<GhostAISystem.Filter>, ISignalOnPowerPelletStart, ISignalOnTrigger2D {
        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public GridMover* Mover;
            public Ghost* Ghost;
            public PhysicsCollider2D* Collider;
        }

        public override void Update(Frame f, ref Filter filter) {
            var entity = filter.Entity;
            var ghost = filter.Ghost;
            var mover = filter.Mover;

            ghost->TimeSinceEaten += f.DeltaTime;
            ghost->SetSpeedMultiplier(mover);

            if (ghost->GhostHouseState != GhostHouseState.NotInGhostHouse) {
                // Don't handle ghost house movement.
                return;
            }

            if (ghost->State is GhostState.Chase or GhostState.Scatter) {
                // Slow down if directly behind another ghost or player
                bool validHit = false;
                HitCollection overlapHits = f.Physics2D.OverlapShape(filter.Transform->Position, 0, filter.Collider->Shape);
                for (int i = 0; i < overlapHits.Count; i++) {
                    Hit overlapHit = overlapHits.HitsBuffer[i];
                    if (entity == overlapHit.Entity || entity.Index < overlapHit.Entity.Index) {
                        // Higher wont slow down...
                        continue;
                    }
                    if (f.Has<Ghost>(overlapHit.Entity)) {
                        // We are overlapping with a ghost. Slow our ass down
                        mover->SpeedMultiplier = FP.FromString("0.85");
                        validHit = true;
                        break;
                    }
                }
                if (!validHit) {
                    Hit? raycastHit = f.Physics2D.Raycast(filter.Transform->Position, mover->DirectionAsVector2(), FP._1_50);
                    if (raycastHit.HasValue) {
                        mover->SpeedMultiplier = FP.FromString("0.85");
                    } else {
                        mover->SpeedMultiplier = 1;
                    }
                }
            }

            var maze = PacmanStageMapData.Current(f).CurrentMazeData(f);
            if (ghost->State == GhostState.Scatter) {
                ghost->TargetPosition = maze.GhostHouse;

                if (FPVector2.DistanceSquared(filter.Transform->Position, maze.GhostHouse) < (10 * 10)) {
                    if ((ghost->ScatterTimer -= f.DeltaTime) <= 0) {
                        ghost->ScatterTimer = f.RNG->Next(10, 15);
                        ghost->State = GhostState.Chase;
                    }
                }

                // ghost->TargetPosition = ghost->Mode switch {
                //     GhostTargetMode.Blinky => maze.Origin + maze.Size + new FPVector2(-5, -5),
                //     GhostTargetMode.Pinky => maze.Origin + FPVector2.Up * maze.Size.Y + new FPVector2(4, -5),
                //     GhostTargetMode.Inky => maze.Origin + FPVector2.Right * maze.Size.X + new FPVector2(-5, 4),
                //     _ => maze.Origin + new FPVector2(4, 4),
                // };
                return;
            }

            // Find the closest player
            var playerFilter = f.Filter<Transform2D, GridMover, PacmanPlayer>();

            FPVector2 currentPosition = filter.Transform->Position;
            FP closestDistance = FP.UseableMax;
            GridMover* closestPlayerMover = null;
            Transform2D* closestPlayerTransform = null;

            while (playerFilter.NextUnsafe(out _, out var playerTransform, out var playerMover, out var pacman)) {
                if (pacman->IsDead) {
                    continue;
                }

                FP distance = FPVector2.DistanceSquared(currentPosition, playerTransform->Position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestPlayerMover = playerMover;
                    closestPlayerTransform = playerTransform;
                }
            }

            ghost->ForceRandomMovement = (closestPlayerMover == null);
            if (closestPlayerMover == null) {
                return;
            }

            // Target based on ghost rules.
            FPVector2 target;
            FPVector2 playerTile = FPVectorUtils.Apply(closestPlayerTransform->Position, FPMath.Round);
            switch (ghost->Mode) {
            default:
            case GhostTargetMode.Blinky:
                // Direct player tile
                target = playerTile;
                break;
            case GhostTargetMode.Pinky:
                // 4 tiles in front of player
                target = playerTile + closestPlayerMover->DirectionAsVector2() * 4;
                break;
            case GhostTargetMode.Inky:
                // Blinky position + (vector between blinky and player position + 2 tiles in front) * 2
                var ghostFilter = f.Filter<Transform2D, Ghost>();
                while (ghostFilter.NextUnsafe(out _, out var otherGhostTransform, out var otherGhost)) {
                    if (otherGhost->Mode != GhostTargetMode.Blinky) {
                        continue;
                    }

                    FPVector2 ourTile = FPVectorUtils.Apply(otherGhostTransform->Position, FPMath.Round);
                    FPVector2 effectivePlayerTile = playerTile + closestPlayerMover->DirectionAsVector2() * 2;
                    FPVector2 vecFromGhostToPlayer = effectivePlayerTile - ourTile;

                    target = effectivePlayerTile + vecFromGhostToPlayer;
                    goto EarlyBreak;
                }

                // Fallback, just target the player.
                target = playerTile;
                break;
            case GhostTargetMode.Clyde:
                // Bottom left if within 8 units (radius), direct player tile otherwise
                if (FPVector2.DistanceSquared(filter.Transform->Position, closestPlayerTransform->Position) > (8 * 8)) {
                    target = playerTile;
                } else {
                    target = maze.Origin + new FPVector2(4, 4);
                }
                break;
            }

            if (ghost->State == GhostState.Chase) {
                if (FPVector2.DistanceSquared(filter.Transform->Position, closestPlayerTransform->Position) < (10 * 10)) {
                    if ((ghost->ScatterTimer -= f.DeltaTime) <= 0) {
                        ghost->ScatterTimer = f.RNG->Next(10, 15);
                        ghost->State = GhostState.Scatter;
                    }
                }
            }

        EarlyBreak:
            // Target the closest player('s tile).
            ghost->TargetPosition = target;
        }

        public void OnPowerPelletStart(Frame f, EntityRef pacman) {
            var filtered = f.Filter<GridMover, Ghost>();
            while (filtered.NextUnsafe(out EntityRef entity, out var mover, out var ghost)) {
                if (ghost->State is GhostState.Chase or GhostState.Scatter) {
                    mover->Direction = (mover->Direction + 2) % 4;
                    ghost->ChangeState(f, entity, GhostState.Scared);
                }
            }
        }

        public void OnTrigger2D(Frame f, TriggerInfo2D info) {
            if (!f.Unsafe.TryGetPointer(info.Entity, out PacmanPlayer* pac) || pac->IsDead) {
                return;
            }

            if (!f.Unsafe.TryGetPointer(info.Entity, out GridMover* pacMover) || pacMover->FreezeTime > 0) {
                return;
            }

            if (!f.Unsafe.TryGetPointer(info.Other, out Ghost* ghost)) {
                return;
            }

            // Pacman, colliding with a ghost.
            switch (ghost->State) {
            case GhostState.Eaten:
                break;

            case GhostState.Scared:
                if (pac->IsDead || !pac->HasPowerPellet) {
                    // Do nothing if we don't have a power pellet.
                    break;
                }
                ghost->ChangeState(f, info.Other, GhostState.Eaten);
                ghost->TimeSinceEaten = 0;
                GhostHouseSystem.ChangeGhostHouseState(f, info.Other, ghost, GhostHouseState.ReturningToEntrance);

                PacmanStageMapData.MazeData maze = PacmanStageMapData.Current(f).CurrentMazeData(f);
                ghost->TargetPosition = maze.GhostHouse + FPVector2.Up * 3;

                f.Signals.OnCharacterEaten(info.Entity, info.Other);
                break;

            case GhostState.Chase:
            case GhostState.Scatter:
                if (!pac->Invincible && ghost->GhostHouseState == GhostHouseState.NotInGhostHouse) {
                    f.Signals.OnPacmanKilled(info.Entity);
                }
                break;
            }
        }
    }
}
