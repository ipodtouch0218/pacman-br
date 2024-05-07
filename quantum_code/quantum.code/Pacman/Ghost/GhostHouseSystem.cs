using Photon.Deterministic;
using Quantum.Collections;
using Quantum.Util;

namespace Quantum.Pacman.Ghost {
    public unsafe class GhostHouseSystem : SystemMainThreadFilter<GhostHouseSystem.Filter>, ISignalOnGridMoverChangeTile {

        public struct Filter {
            public EntityRef Entity;
            public Quantum.Ghost* Ghost;
        }

        public override void OnInit(Frame f) {
            f.Global->GhostHouseQueue = f.AllocateList<EntityRef>(4);
        }

        public override void Update(Frame f, ref Filter filter) {
            if (filter.Ghost->GhostHouseWaitTime > 0) {
                if ((filter.Ghost->GhostHouseWaitTime -= f.DeltaTime) <= 0) {
                    filter.Ghost->GhostHouseWaitTime = 0;
                }
            }
        }

        public void OnGridMoverChangeTile(Frame f, EntityRef entity, FPVector2 tile) {
            if (!f.Unsafe.TryGetPointer(entity, out Quantum.Ghost* ghost)) {
                return;
            }

            if (!f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                return;
            }

            // Ghosts only.
            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);
            int ghostIndex = FPVectorUtils.CellToIndex(tile, f);

            if (ghost->GhostHouseState == GhostHouseState.NotInGhostHouse) {
                return;
            }

            // In the ghost house
            QList<EntityRef> queue = f.ResolveList(f.Global->GhostHouseQueue);
            bool reachedTarget = FPVectorUtils.WorldToIndex(ghost->TargetPosition, f) == FPVectorUtils.CellToIndex(tile, f);
            if (reachedTarget) {
                switch (ghost->GhostHouseState) {
                case GhostHouseState.ReturningToEntrance:
                    ChangeGhostHouseState(f, entity, ghost, GhostHouseState.MovingToCenter);
                    ghost->TargetPosition = mapdata.GhostHouse;
                    break;
                case GhostHouseState.MovingToCenter:
                    FPVector2 ghostHouseCenter = mapdata.GhostHouse;
                    switch (ghost->Mode) {
                    case GhostTargetMode.Inky:
                        ghost->TargetPosition = ghostHouseCenter + FPVector2.Left * 2;
                        ChangeGhostHouseState(f, entity, ghost, GhostHouseState.MovingToSide);
                        break;
                    case GhostTargetMode.Clyde:
                        ghost->TargetPosition = ghostHouseCenter + FPVector2.Right * 2;
                        ChangeGhostHouseState(f, entity, ghost, GhostHouseState.MovingToSide);
                        break;
                    default:
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y + 1;
                        ghost->GhostHouseWaitTime = 1;
                        ghost->ChangeState(f, entity, GhostState.Chase);
                        ChangeGhostHouseState(f, entity, ghost, GhostHouseState.Waiting);
                        break;
                    }
                    break;
                case GhostHouseState.MovingToSide:
                    ghost->TargetPosition.Y = mapdata.GhostHouse.Y + 1;
                    ChangeGhostHouseState(f, entity, ghost, GhostHouseState.Waiting);
                    ghost->GhostHouseWaitTime = 1;
                    ghost->ChangeState(f, entity, GhostState.Chase);
                    break;
                case GhostHouseState.Waiting:
                    bool readyToLeave = ghost->GhostHouseWaitTime <= 0;
                    if (readyToLeave && !queue.Contains(entity)) {
                        queue.Add(entity);
                    }

                    if (queue.IndexOf(entity) == 0) {
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y;
                        ChangeGhostHouseState(f, entity, ghost, GhostHouseState.AlignVertical);
                    } else {
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y + (ghost->TargetPosition.Y > mapdata.GhostHouse.Y ? -1 : 1);
                    }

                    break;
                case GhostHouseState.AlignVertical:
                    if (ghost->TargetPosition.X != mapdata.GhostHouse.X) {
                        ghost->TargetPosition.X = mapdata.GhostHouse.X;
                        ChangeGhostHouseState(f, entity, ghost, GhostHouseState.AlignHorizontal);
                    } else {
                        ghost->TargetPosition = mapdata.GhostHouse + FPVector2.Up * 3;
                        mover->SpeedMultiplier = FP._0_33;
                        ChangeGhostHouseState(f, entity, ghost, GhostHouseState.Leaving);
                    }
                    break;
                case GhostHouseState.AlignHorizontal:
                    ghost->TargetPosition = mapdata.GhostHouse + FPVector2.Up * 3;
                    mover->SpeedMultiplier = FP._0_33;
                    ChangeGhostHouseState(f, entity, ghost, GhostHouseState.Leaving);
                    break;
                case GhostHouseState.Leaving:
                    ghost->SetSpeedMultiplier(mover);
                    ChangeGhostHouseState(f, entity, ghost, GhostHouseState.NotInGhostHouse);
                    queue.Remove(entity);
                    break;
                }
            }
        }

        public static void ChangeGhostHouseState(Frame f, EntityRef entity, Quantum.Ghost* ghost, GhostHouseState state) {
            ghost->GhostHouseState = state;
            f.Events.GhostHouseStateChanged(f, entity, state);
        }
    }
}
