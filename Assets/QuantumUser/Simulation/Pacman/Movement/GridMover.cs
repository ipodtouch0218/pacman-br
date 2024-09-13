using Photon.Deterministic;

namespace Quantum {
    public partial struct GridMover {

        public FPVector2 DirectionAsVector2() {
            return DirectionToVector(Direction);
        }

        public static FPVector2 DirectionToVector(int direction) {
            return direction switch {
                0 => FPVector2.Left,
                1 => FPVector2.Up,
                2 => FPVector2.Right,
                3 => FPVector2.Down,
                _ => FPVector2.Zero,
            };
        }
    }
}
