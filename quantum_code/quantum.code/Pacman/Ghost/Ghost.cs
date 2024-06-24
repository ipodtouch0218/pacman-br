using Photon.Deterministic;

namespace Quantum {
    public unsafe partial struct Ghost {
        public void ChangeState(Frame f, EntityRef entity, GhostState state) {
            if (State == state) {
                return;
            }

            State = state;

            // Modify speed
            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                SetSpeedMultiplier(mover);
            }

            f.Events.GhostStateChanged(f, entity, state);
        }

        public void SetSpeedMultiplier(GridMover* mover) {
            switch (State) {
            case GhostState.Chase:
                if (GhostHouseState == GhostHouseState.NotInGhostHouse) {
                    mover->SpeedMultiplier = 1;
                } else if (GhostHouseState == GhostHouseState.Waiting) {
                    mover->SpeedMultiplier = FP.FromString("0.66");
                }
                break;
            case GhostState.Scared:
                if (GhostHouseState is GhostHouseState.NotInGhostHouse or GhostHouseState.Waiting) {
                    mover->SpeedMultiplier = FP._0_50;
                } else {
                    mover->SpeedMultiplier = FP._0_25;
                }
                break;
            case GhostState.Eaten:
                mover->SpeedMultiplier = 3;
                break;
            }
        }
    }

    public enum GhostTargetMode : byte {
        Blinky,
        Pinky,
        Inky,
        Clyde,
    }

    public enum GhostState : byte {
        Chase,
        Scared,
        Eaten,
    }

    public enum GhostHouseState : byte {
        NotInGhostHouse,
        ReturningToEntrance,
        MovingToCenter,
        MovingToSide,
        Waiting,
        AlignVertical,
        AlignHorizontal,
        Leaving
    }
}
