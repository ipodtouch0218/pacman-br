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

            if (filter.Mover->IsLocked) {
                return;
            }

            //TODO this sucks.
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* link)) {
                // Player movement
                Input input = *f.GetPlayerInput(link->Player);
                if (input.Left.IsDown ^ input.Right.IsDown) {
                    if (input.Left.IsDown) {
                        TryChangeDirection(f, ref filter, 0, FP._0_20);
                    } else if (input.Right.IsDown) {
                        TryChangeDirection(f, ref filter, 2, FP._0_20);
                    }
                }
                if (input.Up.IsDown ^ input.Down.IsDown) {
                    if (input.Up.IsDown) {
                        TryChangeDirection(f, ref filter, 1, FP._0_20);
                    } else if (input.Down.IsDown) {
                        TryChangeDirection(f, ref filter, 3, FP._0_20);
                    }
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

            FPVector2 previousTile = FPVectorUtils.WorldToCell(filter.Transform->Position, f);

            if (CanMoveInDirection(f, ref filter, filter.Mover->Direction)) {
                // Move smoothly
                filter.Transform->Position = MoveInDirection(f, filter.Transform->Position, filter.Mover->Direction, filter.Mover->Speed * filter.Mover->SpeedMultiplier * f.DeltaTime);
                filter.Mover->DistanceMoved += filter.Mover->Speed * filter.Mover->SpeedMultiplier * f.DeltaTime;
            } else {
                // Snap to whole number position
                filter.Transform->Position = FPVectorUtils.Apply(filter.Transform->Position, FPMath.Round);
            }

            FPVector2 newTile = FPVectorUtils.WorldToCell(filter.Transform->Position, f);
            if (previousTile != newTile) {
                f.Signals.OnGridMoverChangeTile(filter.Entity, newTile);
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

        private static bool CanMoveInDirection(Frame f, ref Filter filter, int direction) {
            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset);

            FPVector2 tilePosition = FPVectorUtils.WorldToCell(filter.Transform->Position, f);
            tilePosition += GridMover.DirectionToVector(direction);

            if (tilePosition.X < 0 || tilePosition.X >= mapdata.MapSize.X) {
                return direction == 0 || direction == 2;
            }
            if (tilePosition.Y < 0 || tilePosition.Y >= mapdata.MapSize.Y) {
                return direction == 1 || direction == 3;
            }

                int index = FPVectorUtils.CellToIndex(tilePosition, f);
            if (index == FPVectorUtils.WorldToIndex(mapdata.GhostHouse + (FPVector2.Up * 2), f)) {
                // Ghost house enterance
                bool isEaten = f.TryGet(filter.Entity, out Quantum.Ghost ghost) && ghost.State == GhostState.Eaten;
                return direction != 3 || isEaten;
            }

            return mapdata.CollisionData[index];
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
            tolerance ??= filter.Mover->Speed * filter.Mover->SpeedMultiplier * f.DeltaTime;

            bool notTurnaround = (filter.Mover->Direction % 2) != (newDirection % 2);
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

                FP offset = canCorner ? tolerance.Value : FP._0_01;
                MoveInDirection(ref *filter.Transform, newDirection, offset);
            }
        }

        private static FPVector2 MoveInDirection(Frame f, FPVector2 position, int direction, FP amount) {
            FPVector2 newPosition = position + GridMover.DirectionToVector(direction) * amount;

            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset);
            newPosition.X = FPMath.Repeat(newPosition.X - mapdata.MapOrigin.X, mapdata.MapSize.X) + mapdata.MapOrigin.X;
            newPosition.Y = FPMath.Repeat(newPosition.Y - mapdata.MapOrigin.Y, mapdata.MapSize.Y) + mapdata.MapOrigin.Y;

            return newPosition;
        }

        private static void MoveInDirection(ref Transform2D transform, int direction, FP amount) {
            transform.Position += GridMover.DirectionToVector(direction) * amount;
        }
    }
}
