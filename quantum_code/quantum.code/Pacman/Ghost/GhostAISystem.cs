using Photon.Deterministic;
using Quantum.Util;

namespace Quantum.Pacman.Ghost {
    public unsafe class GhostAISystem : SystemMainThreadFilter<GhostAISystem.Filter>, ISignalOnPowerPelletStart, ISignalOnPowerPelletEnd, ISignalOnTrigger2D {
        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public GridMover* Mover;
            public Quantum.Ghost* Ghost;
        }

        public override void Update(Frame f, ref Filter filter) {

            if (filter.Ghost->GhostHouseState != GhostHouseState.NotInGhostHouse) {
                // Don't handle ghost house movement.
                return;
            }

            // Find the closest player
            var playerFilter = f.Filter<Transform2D, GridMover, PlayerLink>();

            FPVector2 currentPosition = filter.Transform->Position;
            FP closestDistance = FP.UseableMax;
            GridMover? closestPlayerMover = null;
            Transform2D? closestPlayerTransform = null;

            while (playerFilter.Next(out var _, out var playerTransform, out var playerMover, out var _)) {
                FP distance = FPVector2.DistanceSquared(currentPosition, playerTransform.Position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestPlayerMover = playerMover;
                    closestPlayerTransform = playerTransform;
                }
            }

            if (closestPlayerMover == null) {
                return;
            }

            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset);

            FPVector2 target;
            if (filter.Ghost->State == GhostState.Eaten) {
                // Target the ghost house.
                target = mapdata.GhostHouse;

            } else {
                // Target based on ghost rules.
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
                    while (ghostFilter.Next(out var _, out var ghostTransform, out var ghost)) {
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
                        target = mapdata.MapOrigin;
                    }
                    break;
                }
            }

        EarlyBreak:
            // Target the closest player('s tile).
            filter.Ghost->TargetPosition = target;
        }

        public void OnPowerPelletStart(Frame f) {
            var filtered = f.Filter<GridMover, Quantum.Ghost>();
            while (filtered.NextUnsafe(out EntityRef entity, out GridMover* mover, out Quantum.Ghost* ghost)) {
                if (ghost->State == GhostState.Chase) {
                    mover->Direction = (mover->Direction + 2) % 4;
                    ghost->ChangeState(f, entity, GhostState.Scared);
                }
            }
        }

        public void OnPowerPelletEnd(Frame f) {
            var filtered = f.Filter<GridMover, Quantum.Ghost>();
            while (filtered.NextUnsafe(out EntityRef entity, out GridMover* mover, out Quantum.Ghost* ghost)) {
                if (ghost->State == GhostState.Scared) {
                    ghost->ChangeState(f, entity, GhostState.Chase);
                }
            }
        }

        public void OnTrigger2D(Frame f, TriggerInfo2D info) {
            if (!f.Unsafe.TryGetPointer(info.Entity, out PacmanPlayer* pac) || pac->IsDead) {
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
                if (!pac->HasPowerPellet) {
                    // Do nothing if we don't have a power pellet.
                    break;
                }
                ghost->ChangeState(f, info.Other, GhostState.Eaten);
                f.Signals.OnCharacterEaten(info.Entity, info.Other);
                f.Signals.OnGameFreeze(30);
                break;

            case GhostState.Chase:
                if (pac->Invincibility <= 0 && ghost->GhostHouseState == GhostHouseState.NotInGhostHouse) {
                    f.Signals.OnPacmanKilled(info.Entity);
                }
                break;
            }
        }
    }
}
