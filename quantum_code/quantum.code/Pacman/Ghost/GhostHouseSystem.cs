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

            if (ghost->State == GhostState.Eaten) {
                // Ghost entered the ghost house.
                int centerIndex = FPVectorUtils.WorldToIndex(mapdata.GhostHouse, f);
                if (ghostIndex == centerIndex) {
                    ghost->ChangeState(f, entity, GhostState.Chase);
                    ghost->GhostHouseState = GhostHouseState.MovingToSide;

                    FPVector2 ghostHouseCenter = FPVectorUtils.WorldToCell(mapdata.GhostHouse, f);
                    ghost->TargetPosition = ghost->Mode switch {
                        GhostTargetMode.Inky => ghostHouseCenter + FPVector2.Left * 2,
                        GhostTargetMode.Clyde => ghostHouseCenter + FPVector2.Right * 2,
                        _ => ghostHouseCenter,
                    };
                }
            }

            if (ghost->GhostHouseState == GhostHouseState.NotInGhostHouse) {
                return;
            }

            // In the ghost house
            QList<EntityRef> queue = f.ResolveList(f.Global->GhostHouseQueue);
            bool reachedTarget = FPVectorUtils.WorldToIndex(ghost->TargetPosition, f) == FPVectorUtils.CellToIndex(tile, f);
            if (reachedTarget) {
                switch (ghost->GhostHouseState) {
                case GhostHouseState.MovingToSide:
                    ghost->TargetPosition.Y = mapdata.GhostHouse.Y + 1;
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    ghost->GhostHouseWaitTime = 1;
                    break;
                case GhostHouseState.Waiting:
                    bool readyToLeave = ghost->GhostHouseWaitTime <= 0;
                    if (readyToLeave && !queue.Contains(entity)) {
                        queue.Add(entity);
                    }

                    if (queue.IndexOf(entity) == 0) {
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y;
                        ghost->GhostHouseState = GhostHouseState.AlignVertical;
                    } else {
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y + (ghost->TargetPosition.Y > mapdata.GhostHouse.Y ? -1 : 1);
                    }

                    break;
                case GhostHouseState.AlignVertical:
                    if (ghost->TargetPosition.X != mapdata.GhostHouse.X) {
                        ghost->TargetPosition.X = mapdata.GhostHouse.X;
                        ghost->GhostHouseState = GhostHouseState.AlignHorizontal;
                    } else {
                        ghost->TargetPosition = mapdata.GhostHouse + FPVector2.Up * 3;
                        mover->SpeedMultiplier = FP._0_33;
                        ghost->GhostHouseState = GhostHouseState.Leaving;
                    }
                    break;
                case GhostHouseState.AlignHorizontal:
                    ghost->TargetPosition = mapdata.GhostHouse + FPVector2.Up * 3;
                    mover->SpeedMultiplier = FP._0_33;
                    ghost->GhostHouseState = GhostHouseState.Leaving;
                    break;
                case GhostHouseState.Leaving:
                    ghost->SetSpeedMultiplier(mover);
                    ghost->GhostHouseState = GhostHouseState.NotInGhostHouse;
                    queue.Remove(entity);
                    break;
                }
            }
        }
    }
}
