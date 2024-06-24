using Photon.Deterministic;
using Quantum.Physics2D;
using Quantum.Util;

namespace Quantum.Pacman.Ghost {
    public unsafe class GhostAISystem : SystemMainThreadFilter<GhostAISystem.Filter>, ISignalOnPowerPelletStart, ISignalOnTrigger2D {
        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public GridMover* Mover;
            public Quantum.Ghost* Ghost;
        }

        public override void Update(Frame f, ref Filter filter) {

            filter.Ghost->TimeSinceEaten += f.DeltaTime;
            filter.Ghost->SetSpeedMultiplier(filter.Mover);

            if (filter.Ghost->GhostHouseState != GhostHouseState.NotInGhostHouse) {
                // Don't handle ghost house movement.
                return;
            }

            if (filter.Ghost->State == GhostState.Chase) {
                // Slow down if directly behind another ghost or player
                Hit? raycastHit = f.Physics2D.Raycast(filter.Transform->Position, filter.Mover->DirectionAsVector2(), FP._1_50);
                if (raycastHit.HasValue) {
                    filter.Mover->SpeedMultiplier = FP.FromString("0.85");
                } else {
                    HitCollection overlapHits = f.Physics2D.OverlapShape(filter.Transform->Position, 0, f.Get<PhysicsCollider2D>(filter.Entity).Shape);
                    for (int i = 0; i < overlapHits.Count; i++) {
                        Hit overlapHit = overlapHits.HitsBuffer[i];
                        if (filter.Entity == overlapHit.Entity || filter.Entity.Index < overlapHit.Entity.Index) {
                            // Higher wont slow down...
                            continue;
                        }
                        if (f.Has<Quantum.Ghost>(overlapHit.Entity)) {
                            // We are overlapping with a ghost. Slow our ass down
                            filter.Mover->SpeedMultiplier = FP.FromString("0.85");
                            break;
                        }
                    }

                    filter.Mover->SpeedMultiplier = 1;
                }
            }

            var maze = MapCustomData.Current(f).CurrentMazeData(f);
            if (f.Global->GhostsInScatterMode) {
                filter.Ghost->TargetPosition = filter.Ghost->Mode switch {
                    GhostTargetMode.Blinky => maze.Origin + maze.Size + new FPVector2(-5, -5),
                    GhostTargetMode.Pinky => maze.Origin + FPVector2.Up * maze.Size.Y + new FPVector2(4, -5),
                    GhostTargetMode.Inky => maze.Origin + FPVector2.Right * maze.Size.X + new FPVector2(-5, 4),
                    _ => maze.Origin + new FPVector2(4, 4),
                };
                return;
            }

            // Find the closest player
            var playerFilter = f.Filter<Transform2D, GridMover, PacmanPlayer>();

            FPVector2 currentPosition = filter.Transform->Position;
            FP closestDistance = FP.UseableMax;
            GridMover? closestPlayerMover = null;
            Transform2D? closestPlayerTransform = null;

            while (playerFilter.Next(out _, out var playerTransform, out var playerMover, out var pacman)) {
                if (pacman.IsDead) {
                    continue;
                }

                FP distance = FPVector2.DistanceSquared(currentPosition, playerTransform.Position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestPlayerMover = playerMover;
                    closestPlayerTransform = playerTransform;
                }
            }

            filter.Ghost->ForceRandomMovement = (closestPlayerMover == null);
            if (closestPlayerMover == null) {
                return;
            }

            // Target based on ghost rules.
            FPVector2 target;
            FPVector2 playerTile = FPVectorUtils.Apply(closestPlayerTransform.Value.Position, FPMath.Round);
            switch (filter.Ghost->Mode) {
            default:
            case GhostTargetMode.Blinky:
                // Direct player tile
                target = playerTile;
                break;
            case GhostTargetMode.Pinky:
                // 4 tiles in front of player
                target = playerTile + closestPlayerMover.Value.DirectionAsVector2() * 4;
                break;
            case GhostTargetMode.Inky:
                // Blinky position + (vector between blinky and player position + 2 tiles in front) * 2
                var ghostFilter = f.Filter<Transform2D, Quantum.Ghost>();
                while (ghostFilter.Next(out _, out var ghostTransform, out var ghost)) {
                    if (ghost.Mode != GhostTargetMode.Blinky) {
                        continue;
                    }

                    FPVector2 ourTile = FPVectorUtils.Apply(ghostTransform.Position, FPMath.Round);
                    FPVector2 effectivePlayerTile = playerTile + closestPlayerMover.Value.DirectionAsVector2() * 2;
                    FPVector2 vecFromGhostToPlayer = effectivePlayerTile - ourTile;

                    target = effectivePlayerTile + vecFromGhostToPlayer;
                    goto EarlyBreak;
                }

                // Fallback, just target the player.
                target = playerTile;
                break;
            case GhostTargetMode.Clyde:
                // Bottom left if within 8 units (radius), direct player tile otherwise
                if (FPVector2.DistanceSquared(filter.Transform->Position, closestPlayerTransform.Value.Position) > (8 * 8)) {
                    target = playerTile;
                } else {
                    target = maze.Origin + new FPVector2(4, 4);
                }
                break;
            }

        EarlyBreak:
            // Target the closest player('s tile).
            filter.Ghost->TargetPosition = target;
        }

        public void OnPowerPelletStart(Frame f, EntityRef pacman) {
            var filtered = f.Filter<GridMover, Quantum.Ghost>();
            while (filtered.NextUnsafe(out EntityRef entity, out GridMover* mover, out Quantum.Ghost* ghost)) {
                if (ghost->State == GhostState.Chase) {
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

            if (!f.Unsafe.TryGetPointer(info.Other, out Quantum.Ghost* ghost)) {
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

                MapCustomData.MazeData maze = MapCustomData.Current(f).CurrentMazeData(f);
                ghost->TargetPosition = maze.GhostHouse + FPVector2.Up * 3;

                f.Signals.OnCharacterEaten(info.Entity, info.Other);
                break;

            case GhostState.Chase:
                if (!pac->Invincible && ghost->GhostHouseState == GhostHouseState.NotInGhostHouse) {
                    f.Signals.OnPacmanKilled(info.Entity);
                }
                break;
            }
        }
    }
}
