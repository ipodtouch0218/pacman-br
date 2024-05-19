using Photon.Deterministic;

namespace Quantum.Pacman.Logic {
    public unsafe class GameLogicSystem : SystemMainThread, ISignalOnPlayerReady {

        public override void OnInit(Frame f) {
            // Temporary...
            f.Global->Timer = 180;
        }

        public override void Update(Frame f) {
            if (f.Global->GameStartingTimer > 0) {
                // Start game!
                if ((f.Global->GameStartingTimer -= f.DeltaTime) <= 0) {
                    f.Global->GameStarted = true;
                    f.Global->Timer = 180;
                    f.Global->GameStartTick = f.Number;

                    f.SystemEnable<PausableSystemGroup>();
                    f.Events.GameStart();
                }
            } else if (f.Global->GameStarted) {
                // End game!
                if ((f.Global->Timer -= f.DeltaTime) <= 0) {
                    f.Global->GameStarted = false;
                    f.Global->Timer = 0;
                    f.SystemDisable<PausableSystemGroup>();

                    f.Events.GameEnd();
                }
            } else {
                // Waiting for players to ready up...
            }
        }

        public void OnPlayerReady(Frame f, PlayerRef player) {
            var playerLinks = f.Filter<PlayerLink>();
            while (playerLinks.Next(out _, out PlayerLink playerLink)) {
                if (!playerLink.ReadyToPlay) {
                    return;
                }
            }

            // All players are ready!
            f.Global->GameStartingTimer = FP._5;
            f.Events.GameStarting();
        }
    }
}
