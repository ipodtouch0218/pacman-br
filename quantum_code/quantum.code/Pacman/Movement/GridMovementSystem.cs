using Photon.Deterministic;
using Quantum.Util;
using System.Collections.Generic;

namespace Quantum.Pacman.Ghost {
    public unsafe class GridMovementSystem : SystemMainThreadFilter<GridMovementSystem.Filter> {

        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public GridMover* Mover;
        }

        public override void Update(Frame f, ref Filter filter) {

            if (filter.Mover->TeleportFrames > 0) {
                if (--filter.Mover->TeleportFrames == 0) {
                    f.Events.TeleportEvent(filter.Entity, false);
                }
            }

            if (filter.Mover->IsLocked) {
                return;
            }

            if (filter.Mover->FreezeTime > 0) {
                if ((filter.Mover->FreezeTime -= f.DeltaTime) <= 0) {
                    filter.Mover->FreezeTime = 0;
                }
                return;
            }

            //TODO this sucks.
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* link)) {
                // Player movement
                Input input = *f.GetPlayerInput(link->Player);
                if (input.TargetDirection != -1) {
                    TryChangeDirection(f, ref filter, input.TargetDirection, FP._0_20);
                }
            } else if (f.Unsafe.TryGetPointer(filter.Entity, out Quantum.Ghost* ghost)) {
                // AI movement
                if ((ghost->State == GhostState.Scared || ghost->ForceRandomMovement) && ghost->GhostHouseState == GhostHouseState.NotInGhostHouse) {
                    // Random movement
                    int nextDirection = (filter.Mover->Direction + 2) % 4;
                    List<int> possibleDirections = new();
                    for (int i = 0; i < 4; i++) {
                        if (i == nextDirection) {
                            continue;
                        }
                        if (CanMoveInDirection(f, ref filter, i)) {
                            possibleDirections.Add(i);
                        }
                    }
                    if (possibleDirections.Count > 0) {
                        nextDirection = possibleDirections[f.Global->RngSession.Next(0, possibleDirections.Count)];
                    }
                    TryChangeDirection(f, ref filter, nextDirection);
                } else {
                    // Go to target movement
                    int reverseDirection = (filter.Mover->Direction + 2) % 4;

                    FP minDistanceToTarget = FP.UseableMax;
                    int bestDirection = reverseDirection;
                    for (int i = 0; i < 4; i++) {
                        if (ghost->GhostHouseState <= GhostHouseState.ReturningToEntrance && i == reverseDirection) {
                            continue;
                        }

                        FP distance = SquaredDistanceToTargetAfterMove(f, ref filter, i, ghost->TargetPosition);
                        if (distance < minDistanceToTarget) {
                            minDistanceToTarget = distance;
                            bestDirection = i;
                        }
                    }

                    TryChangeDirection(f, ref filter, bestDirection);
                }
            }

            FPVector2 previousPosition = filter.Transform->Position;
            FPVector2 previousTile = FPVectorUtils.WorldToCell(previousPosition, f);
            filter.Mover->IsStationary = !CanMoveInDirection(f, ref filter, filter.Mover->Direction);
            if (!filter.Mover->IsStationary) {
                // Move smoothly
                var moveResult = MoveInDirection(f, previousPosition, filter.Mover->Direction, f.Global->GameSpeed * filter.Mover->SpeedMultiplier * f.DeltaTime);
                filter.Transform->Position = moveResult.NewPosition;
                filter.Mover->DistanceMoved += f.Global->GameSpeed * filter.Mover->SpeedMultiplier * f.DeltaTime;

                if (moveResult.Teleported) {
                    f.Events.TeleportEvent(filter.Entity, true);
                    filter.Mover->TeleportFrames = 2;
                }

            } else {
                // Snap to whole number position
                filter.Transform->Position = FPVectorUtils.Apply(previousPosition, FPMath.Round);
            }

            // Changed tile
            FPVector2 newTile = FPVectorUtils.WorldToCell(filter.Transform->Position, f);
            if (previousTile != newTile) {
                f.Signals.OnGridMoverChangeTile(filter.Entity, newTile);
                f.Events.GridMoverChangeTile(filter.Entity, newTile);
            }

            // Crossed center of tile
            if (FPVectorUtils.Apply(previousPosition * 2, FPMath.Round) != FPVectorUtils.Apply(filter.Transform->Position * 2, FPMath.Round)) {
                f.Events.GridMoverReachedCenterOfTile(filter.Entity, newTile);
            }
        }

        private static FP SquaredDistanceToTargetAfterMove(Frame f, ref Filter filter, int direction, FPVector2 target) {

            if (!CanMoveInDirection(f, ref filter, direction)) {
                return FP.UseableMax;
            }

            FPVector2 origin = FPVectorUtils.Apply(filter.Transform->Position, FPMath.Round);
            FPVector2 nextPosition = origin + GridMover.DirectionToVector(direction);

            return FPVector2.DistanceSquared(nextPosition, target);
        }

        public static bool CanMoveInDirection(Frame f, ref Filter filter, int direction) {
            MapCustomData.MazeData maze = MapCustomData.Current(f).CurrentMazeData(f);

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
                bool isEaten = f.TryGet(filter.Entity, out Quantum.Ghost ghost) && ghost.State == GhostState.Eaten;
                return direction != 3 || isEaten;
            }

            return maze.CollisionData[index];
        }

        private static void TryChangeDirection(Frame f, ref Filter filter, int newDirection, FP? tolerance = null) {

            if (newDirection == filter.Mover->Direction) {
                return;
            }

            // Don't allow turning into a wall if we are already moving in that direction
            if (CanMoveInDirection(f, ref filter, filter.Mover->Direction) && !CanMoveInDirection(f, ref filter, newDirection)) {
                return;
            }

            bool canCorner = tolerance.HasValue;
            tolerance ??= f.Global->GameSpeed * filter.Mover->SpeedMultiplier * f.DeltaTime;

            bool notTurnaround = (filter.Mover->Direction != -1) && ((filter.Mover->Direction % 2) != (newDirection % 2));
            FPVector2 center = FPVectorUtils.Apply(filter.Transform->Position, FPMath.Round);

            if (notTurnaround) {
                FP distanceToCenter = FPVector2.DistanceSquared(filter.Transform->Position, center);
                if (distanceToCenter >= (tolerance.Value * tolerance.Value)) {
                    // Too far away to turn
                    return;
                }
            }

            filter.Mover->Direction = newDirection;

            if (notTurnaround) {
                filter.Transform->Position = center;
                MoveInDirection(ref *filter.Transform, newDirection, FP._0_01);
            }
        }

        struct MovementResult {
            public FPVector2 NewPosition;
            public bool Teleported;
        }

        private static MovementResult MoveInDirection(Frame f, FPVector2 position, int direction, FP amount) {
            MapCustomData.MazeData maze = MapCustomData.Current(f).CurrentMazeData(f);

            FPVector2 newPosition = position + GridMover.DirectionToVector(direction) * amount;
            FPVector2 wrappedPosition = default;
            wrappedPosition.X = RepeatBetween(newPosition.X - maze.Origin.X, -1, maze.Size.X) + maze.Origin.X;
            wrappedPosition.Y = RepeatBetween(newPosition.Y - maze.Origin.Y, -1, maze.Size.Y) + maze.Origin.Y;

            return new() {
                NewPosition = wrappedPosition,
                Teleported = newPosition != wrappedPosition
            };
        }

        private static void MoveInDirection(ref Transform2D transform, int direction, FP amount) {
            transform.Position += GridMover.DirectionToVector(direction) * amount;
        }

        private static FP RepeatBetween(FP value, FP min, FP max) {
            return FPMath.Repeat(value - min, max - min) + min;
        }
    }
}
