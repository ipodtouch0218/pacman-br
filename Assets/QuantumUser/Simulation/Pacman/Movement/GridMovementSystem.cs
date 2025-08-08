using Photon.Deterministic;
using Quantum.Util;
using UnityEngine;

namespace Quantum.Pacman.Ghosts {
    public unsafe class GridMovementSystem : SystemMainThreadFilter<GridMovementSystem.Filter> {

        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public GridMover* Mover;
        }

        public override void Update(Frame f, ref Filter filter) {
            var entity = filter.Entity;
            var mover = filter.Mover;

            if (mover->TeleportFrames > 0) {
                if (--mover->TeleportFrames == 0) {
                    f.Events.TeleportEvent(entity, false);
                }
            }

            if (mover->IsLocked) {
                return;
            }

            if (mover->FreezeTime > 0) {
                if ((mover->FreezeTime -= f.DeltaTime) <= 0) {
                    mover->FreezeTime = 0;
                }
                return;
            }

            var transform = filter.Transform;

            FPVector2 previousPosition = transform->Position;
            FPVector2 previousTile = FPVectorUtils.WorldToCell(previousPosition, f);

            bool wasStationary = mover->IsStationary;
            mover->IsStationary = !CanMoveInDirection(f, ref filter, mover->Direction);
            bool hitWall = !wasStationary && mover->IsStationary;
            if (!mover->IsStationary) {
                // Move smoothly
                FP distance = f.Global->GameSpeed * mover->SpeedMultiplier * f.DeltaTime;
                var moveResult = MoveInDirection(f, previousPosition, mover->Direction, distance);
                mover->DistanceMoved += distance;

                if (moveResult.Teleported) {
                    transform->Teleport(f, moveResult.NewPosition);
                    f.Events.TeleportEvent(entity, true);
                    mover->TeleportFrames = 2;
                } else {
                    transform->Position = moveResult.NewPosition;
                }
            } else {
                // Snap to whole number position
                transform->Position = FPVectorUtils.Apply(previousPosition, FPMath.Round);
            }

            // Changed tile
            FPVector2 newTile = FPVectorUtils.WorldToCell(transform->Position, f);
            if (previousTile != newTile) {
                f.Signals.OnGridMoverChangeTile(entity, newTile);
                f.Events.GridMoverChangeTile(entity, newTile);
            }

            // Crossed center of tile
            if (hitWall || FPVectorUtils.Apply(previousPosition + (FPVector2.One / 2), FPMath.Round) != FPVectorUtils.Apply(transform->Position + (FPVector2.One / 2), FPMath.Round)) {
                f.Events.GridMoverReachedCenterOfTile(entity, newTile);
                f.Signals.OnGridMoverReachedCenterOfTile(entity, newTile);
            }
        }

        public static FP SquaredDistanceToTargetAfterMove(Frame f, ref Filter filter, int direction, FPVector2 target) {
            if (!CanMoveInDirection(f, ref filter, direction)) {
                return FP.UseableMax;
            }

            FPVector2 origin = FPVectorUtils.Apply(filter.Transform->Position, FPMath.Round);
            FPVector2 nextPosition = origin + GridMover.DirectionToVector(direction);

            return FPVector2.DistanceSquared(nextPosition, target);
        }

        public static bool CanMoveInDirection(Frame f, ref Filter filter, int direction) {
            PacmanStageMapData.MazeData maze = PacmanStageMapData.Current(f).CurrentMazeData(f);

            FPVector2 tilePosition = FPVectorUtils.WorldToCell(filter.Transform->Position, f);
            tilePosition += GridMover.DirectionToVector(direction);

            if (tilePosition.X < 0 || tilePosition.X >= maze.Size.X) {
                return direction == 0 || direction == 2;
            }
            if (tilePosition.Y < 0 || tilePosition.Y >= maze.Size.Y) {
                return direction == 1 || direction == 3;
            }

            int index = FPVectorUtils.CellToIndex(tilePosition, f);
            if (index == FPVectorUtils.WorldToIndex(maze.GhostHouse + (FPVector2.Up * 2), f)) {
                // Ghost house enterance
                bool isEaten = f.Unsafe.TryGetPointer(filter.Entity, out Ghost* ghost) && ghost->State == GhostState.Eaten;
                return direction != 3 || isEaten;
            }

            return maze.CollisionData[index];
        }

        public static void TryChangeDirection(Frame f, ref Filter filter, int newDirection, FP? tolerance = null) {
            var mover = filter.Mover;

            if (newDirection == mover->Direction) {
                return;
            }

            // Don't allow turning into a wall if we are already moving in that direction
            if (/*CanMoveInDirection(f, ref filter, mover->Direction) &&*/ !CanMoveInDirection(f, ref filter, newDirection)) {
                return;
            }

            var transform = filter.Transform;
            bool canCorner = tolerance.HasValue;

            bool turnaround = (mover->Direction != -1) && ((mover->Direction % 2) == (newDirection % 2));

            FPVector2 center = FPVectorUtils.Apply(transform->Position, FPMath.Round);
            tolerance ??= f.Global->GameSpeed * mover->SpeedMultiplier * f.DeltaTime;

            if (!turnaround) {
                FP distanceToCenter = FPVector2.DistanceSquared(transform->Position, center);
                if (distanceToCenter >= (tolerance.Value * tolerance.Value)) {
                    // Too far away to turn
                    return;
                }
            }

            filter.Mover->Direction = newDirection;

            if (!turnaround) {
                filter.Transform->Position = center;
                MoveInDirection(transform, newDirection, (tolerance ?? FP._0_02) - FP._0_01);
            }
        }

        public struct MovementResult {
            public FPVector2 NewPosition;
            public bool Teleported;
        }

        public static MovementResult MoveInDirection(Frame f, FPVector2 position, int direction, FP amount) {
            PacmanStageMapData.MazeData maze = PacmanStageMapData.Current(f).CurrentMazeData(f);

            FPVector2 newPosition = position + GridMover.DirectionToVector(direction) * amount;
            FPVector2 wrappedPosition = default;
            wrappedPosition.X = RepeatBetween(newPosition.X - maze.Origin.X, -1, maze.Size.X) + maze.Origin.X;
            wrappedPosition.Y = RepeatBetween(newPosition.Y - maze.Origin.Y, -1, maze.Size.Y) + maze.Origin.Y;

            return new() {
                NewPosition = wrappedPosition,
                Teleported = newPosition != wrappedPosition
            };
        }

        private static void MoveInDirection(Transform2D* transform, int direction, FP amount) {
            transform->Position += GridMover.DirectionToVector(direction) * amount;
        }

        private static FP RepeatBetween(FP value, FP min, FP max) {
            return FPMath.Repeat(value - min, max - min) + min;
        }
    }
}
