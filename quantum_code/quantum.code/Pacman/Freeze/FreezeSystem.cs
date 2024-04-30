using Quantum.Pacman.Freeze;

namespace Quantum.Pacman {
    public unsafe class FreezeSystem : SystemMainThread, ISignalOnGameFreeze {

        public override void OnInit(Frame f) {
            f.Signals.OnGameFreeze(30 * 5);
        }

        public override void Update(Frame f) {
            if (f.Global->FreezeDuration > 0) {
                if (--f.Global->FreezeDuration == 0) {
                    f.SystemEnable<FreezableSystemGroup>();
                    f.Events.GameUnfreeze();
                } else {
                    f.SystemDisable<FreezableSystemGroup>();
                }
            } else {
                f.Global->TimeSinceGameStart += f.DeltaTime;
            }
        }

        public void OnGameFreeze(Frame f, int duration) {
            f.Global->FreezeDuration = duration;
            f.Events.GameFreeze(duration);
        }
    }
}
