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

            f.Events.GhostStateChanged(entity, state);
        }

        public void SetSpeedMultiplier(GridMover* mover) {
            mover->SpeedMultiplier = State switch {
                GhostState.Scared => FP._0_50,
                GhostState.Eaten => 4,
                _ => 1
            };
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
        MovingToSide,
        Waiting,
        AlignVertical,
        AlignHorizontal,
        Leaving
    }
}
