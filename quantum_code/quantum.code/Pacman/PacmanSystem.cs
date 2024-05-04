using Photon.Deterministic;

namespace Quantum.Pacman.Ghost {
    public unsafe class PacmanSystem : SystemMainThreadFilter<PacmanSystem.Filter>, ISignalOnPacmanScored, ISignalOnCharacterEaten, ISignalOnPacmanKilled, ISignalOnPacmanRespawned, ISignalOnPowerPelletStart, ISignalOnPowerPelletEnd, ISignalOnTrigger2D {

        public struct Filter {
            public EntityRef Entity;
            public PacmanPlayer* Pacman;
        }

        public override void Update(Frame f, ref Filter filter) {
            if (filter.Pacman->IsDead) {
                if (filter.Pacman->RespawnTimer > 0) {
                    if ((filter.Pacman->RespawnTimer -= f.DeltaTime) <= 0) {
                        filter.Pacman->RespawnTimer = 0;
                        f.Signals.OnPacmanRespawned(filter.Entity);
                    }
                }
            }

            if (filter.Pacman->Invincibility > 0) {
                if ((filter.Pacman->Invincibility -= f.DeltaTime) <= 0) {
                    filter.Pacman->Invincibility = 0;
                    f.Events.PacmanVulnerable(filter.Entity);
                }
            }
        }

        public void OnPowerPelletStart(Frame f) {
            var filter = f.Filter<PacmanPlayer>();

            while (filter.NextUnsafe(out _, out PacmanPlayer* pacman)) {
                pacman->GhostCombo = 0;
            }
        }

        public void OnPowerPelletEnd(Frame f) {
            var filter = f.Filter<PacmanPlayer>();

            while (filter.NextUnsafe(out _, out PacmanPlayer* pacman)) {
                pacman->HasPowerPellet = false;
            }
        }

        public void OnPacmanRespawned(Frame f, EntityRef entity) {
            if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pac)) {
                pac->IsDead = false;
            }

            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                mover->IsLocked = false;
            }

            pac->Invincibility = 5;
            f.Events.PacmanRespawned(entity);
        }

        public void OnPacmanKilled(Frame f, EntityRef entity) {
            if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pac)) {
                pac->IsDead = true;
            }

            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                mover->IsLocked = true;
            }

            pac->RespawnTimer = 5;
            f.Events.PacmanKilled(entity, pac->RespawnTimer);
        }

        public void OnTrigger2D(Frame f, TriggerInfo2D info) {
            if (!f.Unsafe.TryGetPointer(info.Entity, out PacmanPlayer* pac1) || pac1->IsDead) {
                return;
            }

            if (!f.TryGet(info.Other, out PacmanParent parent) || !f.Unsafe.TryGetPointer(parent.Entity, out PacmanPlayer* pac2) || pac2->IsDead) {
                return;
            }

            if (pac1 == pac2) {
                return;
            }

            // Two PacmanPlayers collided
            if (pac1->HasPowerPellet && !pac2->HasPowerPellet && pac2->Invincibility <= 0) {
                // Pac1 eaten Pac2
                f.Signals.OnCharacterEaten(info.Entity, parent.Entity);
                f.Signals.OnPacmanKilled(parent.Entity);
                f.Signals.OnGameFreeze(FP._0_50);
            }

            if (pac2->HasPowerPellet && !pac1->HasPowerPellet && pac1->Invincibility <= 0) {
                // Pac2 eaten Pac1
                f.Signals.OnCharacterEaten(parent.Entity, info.Entity);
                f.Signals.OnPacmanKilled(info.Entity);
                f.Signals.OnGameFreeze(FP._0_50);
            }
        }

        public void OnCharacterEaten(Frame f, EntityRef pacmanEntity, EntityRef other) {
            if (!f.Unsafe.TryGetPointer(pacmanEntity, out PacmanPlayer* pacman)) {
                return;
            }

            int points = (++pacman->GhostCombo) switch {
                1 => 200,
                2 => 400,
                3 => 800,
                4 => 1600,
                5 => 3200,
                _ => 7650,
            };

            f.Signals.OnPacmanScored(pacmanEntity, points);
            f.Events.CharacterEaten(f, pacmanEntity, other, pacman->GhostCombo, points);
        }

        public void OnPacmanScored(Frame f, EntityRef pacmanEntity, int points) {
            if (!f.Unsafe.TryGetPointer(pacmanEntity, out PacmanPlayer* pacman)) {
                return;
            }

            pacman->Score += points;

            f.Events.PacmanScored(pacmanEntity, points, pacman->Score);
        }
    }
}
