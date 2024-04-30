using Photon.Deterministic;
using Quantum.Util;

namespace Quantum.Pacman.Ghost {
    public unsafe class GhostHouseSystem : SystemSignalsOnly, ISignalOnGridMoverChangeTile {

        public override void OnInit(Frame f) {
            f.Global->CanLeaveGhostHouse = true;
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

                    FPVector2 ghostHouseCenter = ghost->TargetPosition;
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
            bool reachedTarget = FPVectorUtils.WorldToIndex(ghost->TargetPosition, f) == FPVectorUtils.CellToIndex(tile, f);
            if (reachedTarget) {
                switch (ghost->GhostHouseState) {
                case GhostHouseState.MovingToSide:
                    ghost->TargetPosition.Y = mapdata.GhostHouse.Y + 1;
                    ghost->GhostHouseState = GhostHouseState.Waiting;
                    break;
                case GhostHouseState.Waiting:
                    FP timeUntilLeave = ghost->Mode switch {
                        GhostTargetMode.Blinky => 0,
                        GhostTargetMode.Pinky => 5,
                        GhostTargetMode.Inky => 15,
                        _ => 25
                    };

                    if (f.Global->TimeSinceGameStart > timeUntilLeave && f.Global->CanLeaveGhostHouse) {
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y;
                        ghost->GhostHouseState = GhostHouseState.AlignVertical;
                        f.Global->CanLeaveGhostHouse = false;
                    } else {
                        ghost->TargetPosition.Y = mapdata.GhostHouse.Y + (ghost->TargetPosition.Y > mapdata.GhostHouse.Y ? -1 : 1);
                    }
                    break;
                case GhostHouseState.AlignVertical:
                    if (ghost->TargetPosition.X != mapdata.GhostHouse.X) {
                        ghost->TargetPosition.X = mapdata.GhostHouse.X;
                        ghost->GhostHouseState = GhostHouseState.AlignHorizontal;
                    } else {
                        ghost->TargetPosition = mapdata.GhostHouse + FPVector2.Up * 4;
                        mover->SpeedMultiplier = FP._0_50;
                        ghost->GhostHouseState = GhostHouseState.Leaving;
                    }
                    break;
                case GhostHouseState.AlignHorizontal:
                    ghost->TargetPosition = mapdata.GhostHouse + FPVector2.Up * 4;
                    mover->SpeedMultiplier = FP._0_50;
                    ghost->GhostHouseState = GhostHouseState.Leaving;
                    break;
                case GhostHouseState.Leaving:
                    ghost->SetSpeedMultiplier(mover);
                    ghost->GhostHouseState = GhostHouseState.NotInGhostHouse;
                    f.Global->CanLeaveGhostHouse = true;
                    break;
                }
            }
        }
    }
}
