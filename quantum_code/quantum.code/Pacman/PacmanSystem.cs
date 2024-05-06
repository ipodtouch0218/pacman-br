using Photon.Deterministic;

namespace Quantum.Pacman.Ghost {
    public unsafe class PacmanSystem : SystemMainThreadFilter<PacmanSystem.Filter>, ISignalOnPacmanScored, ISignalOnCharacterEaten, ISignalOnPacmanKilled, ISignalOnPacmanRespawned, ISignalOnPowerPelletStart, ISignalOnPowerPelletEnd {

        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public PhysicsCollider2D* Collider;
            public GridMover* Mover;
            public PacmanPlayer* Pacman;
        }

        public override void Update(Frame f, ref Filter filter) {
            PacmanPlayer* pac = filter.Pacman;

            if (pac->IsDead) {
                if (pac->RespawnTimer > 0) {
                    if ((pac->RespawnTimer -= f.DeltaTime) <= 0) {
                        pac->RespawnTimer = 0;
                        f.Signals.OnPacmanRespawned(filter.Entity);
                    }
                }
            }

            if (pac->Invincibility > 0) {
                if ((pac->Invincibility -= f.DeltaTime) <= 0) {
                    pac->Invincibility = 0;
                    f.Events.PacmanVulnerable(filter.Entity);
                }
            }

            if (pac->HasPowerPellet) {
                var hits = f.Physics2D.OverlapShape(*filter.Transform, filter.Collider->Shape);
                for (int i = 0; i < hits.Count; i++) {
                    var hit = hits[i];

                    if (!hit.Entity.IsValid || hit.Entity == filter.Entity) {
                        continue;
                    }

                    if (!f.Unsafe.TryGetPointer(hit.Entity, out PacmanPlayer* otherPac) || otherPac->IsDead) {
                        continue;
                    }

                    // We collided with a PacmanPlayer
                    if (pac->HasPowerPellet && !otherPac->HasPowerPellet && otherPac->Invincibility <= 0) {
                        // Pac1 eaten Pac2
                        f.Signals.OnCharacterEaten(filter.Entity, hit.Entity);
                        f.Signals.OnPacmanKilled(hit.Entity);
                        //f.Signals.OnGameFreeze(FP._0_50);
                        filter.Mover->FreezeTime = FP._0_50;
                    }
                }
            }
        }

        public void OnPowerPelletStart(Frame f) {
            var filter = f.Filter<PacmanPlayer, GridMover>();

            while (filter.NextUnsafe(out _, out PacmanPlayer* pacman, out GridMover* mover)) {
                pacman->GhostCombo = 0;
                if (!pacman->HasPowerPellet) {
                    mover->SpeedMultiplier = FP.FromString("0.85");
                }
            }
        }

        public void OnPowerPelletEnd(Frame f) {
            var filter = f.Filter<PacmanPlayer, GridMover>();

            while (filter.NextUnsafe(out _, out PacmanPlayer* pacman, out GridMover* mover)) {
                pacman->HasPowerPellet = false;
                mover->SpeedMultiplier = FP._1;
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

        public void OnCharacterEaten(Frame f, EntityRef pacmanEntity, EntityRef other) {
            if (!f.Unsafe.TryGetPointer(pacmanEntity, out PacmanPlayer* pacman)) {
                return;
            }

            int points = (++pacman->GhostCombo) switch {
                1 => 400,
                2 => 800,
                3 => 1200,
                4 => 1600,
                5 => 2000,
                6 => 2400,
                7 => 2800,
                _ => 3200,
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
