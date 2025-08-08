using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Pacman.Ghosts {
    public unsafe class PacmanSystem : SystemMainThreadFilter<PacmanSystem.Filter>, ISignalOnPacmanScored, ISignalOnCharacterEaten, ISignalOnPacmanKilled, ISignalOnPacmanRespawned, ISignalOnPowerPelletStart, ISignalOnPowerPelletEnd {

        public static readonly FP BombBaseTravelTime = FP.FromString("0.225");
        public static readonly FP BombDistanceTravelTime = FP.FromString("0.0075");

        public struct Filter {
            public EntityRef Entity;
            public PacmanPlayer* Pacman;
            public GridMover* Mover;
            public Transform2D* Transform;
            public PhysicsCollider2D* Collider;
        }

        public override void Update(Frame f, ref Filter filter) {
            var entity = filter.Entity;
            var pacman = filter.Pacman;
            var mover = filter.Mover;
            var transform = filter.Transform;
            
            if (pacman->IsDead) {
                if (pacman->RespawnTimer > 0) {
                    if ((pacman->RespawnTimer -= f.DeltaTime) <= 0) {
                        pacman->RespawnTimer = 0;
                        f.Signals.OnPacmanRespawned(entity);
                    }
                }
            }

            // Full Invincibility
            if (pacman->Invincibility > 0) {
                if ((pacman->Invincibility -= f.DeltaTime) <= 0) {
                    pacman->Invincibility = 0;
                    f.Events.PacmanVulnerable(entity);
                }
            }

            Input input = default;
            if (f.Unsafe.TryGetPointer(entity, out PlayerLink* pl)) {
                input = *f.GetPlayerInput(pl->Player);
            } else {
                // Bot input, potentially??
            }

            if (pacman->IsDead) {
                if (pacman->RespawnTimer >= FP.FromString("4.9")) {
                    if (pacman->Bombs > 0 && input.Bomb.WasPressed) {
                        // Allow bomb use 100ms after dying
                        UseBomb(f, ref filter);
                        pacman->RespawnTimer = 0;
                        pacman->IsDead = false;
                        mover->IsLocked = false;
                    }
                } else {
                    if (pacman->RespawnTimer + f.DeltaTime >= FP.FromString("4.9")) {
                        if (pacman->HasPowerPellet) {
                            pacman->PowerPelletTimer = 0;
                            f.Signals.OnPowerPelletEnd(entity);
                            f.Events.PowerPelletEnd(entity);
                        }
                        f.Events.PacmanKilled(entity, pacman->RespawnTimer);
                    }
                }
                return;
            }


            if (f.Unsafe.TryGetPointer(entity, out PlayerLink* link)) {
                // Player movement
                if (input.TargetDirection != -1 && mover->Direction != input.TargetDirection) {
                    f.Unsafe.ComponentGetter<GridMovementSystem.Filter>().TryGet(f, entity, out var gridFilter);
                    GridMovementSystem.TryChangeDirection(f, ref gridFilter, input.TargetDirection, FP._0_33);
                }
            }

            // Bombs
            if (pacman->BombTravelTimer > 0) {
                // Flying through the air. Gracefully.
                if ((pacman->BombTravelTimer -= f.DeltaTime) <= 0) {
                    pacman->BombTravelTimer = 0;
                    transform->Position = pacman->BombEndPosition;
                    f.Events.PacmanLandBombJump(entity);

                } else {
                    FP alpha = 1 - (pacman->BombTravelTimer / pacman->BombTravelTime);

                    FPVector2 newPos = FPVector2.Lerp(pacman->BombStartPosition, pacman->BombEndPosition, alpha);

                    var map = PacmanStageMapData.Current(f);
                    newPos += FPVector2.Up * map.BombHeightCurve.Evaluate(alpha);

                    transform->Position = newPos;
                    return;
                }
            } else if (pacman->Bombs > 0 && mover->FreezeTime <= 0) {
                // Check for bomb input
                if (input.Bomb.WasPressed) {
                    UseBomb(f, ref filter);
                    return;
                }
            }

            if (mover->FreezeTime > 0) {
                return;
            }

            // Temporary Invincibility
            if (pacman->TemporaryInvincibility > 0) {
                if ((pacman->TemporaryInvincibility -= f.DeltaTime) <= 0) {
                    pacman->TemporaryInvincibility = 0;
                }
            }

            // Power Pellet
            if (pacman->HasPowerPellet) {
                var hits = f.Physics2D.OverlapShape(*transform, filter.Collider->Shape);
                for (int i = 0; i < hits.Count; i++) {
                    var hit = hits[i];

                    if (hit.Entity == entity || !f.Exists(hit.Entity)) {
                        continue;
                    }

                    if (!f.Unsafe.TryGetPointer(hit.Entity, out PacmanPlayer* otherPac) || otherPac->IsDead) {
                        continue;
                    }

                    // We collided with a PacmanPlayer
                    if (!otherPac->HasPowerPellet && !otherPac->Invincible) {
                        // Pac1 eaten Pac2
                        f.Signals.OnCharacterEaten(entity, hit.Entity);
                        f.Signals.OnPacmanKilled(hit.Entity);
                    }
                }
            }
        }

        public static void UseBomb(Frame f, ref Filter filter) {
            var entity = filter.Entity;
            var pacman = filter.Pacman;
            var transform = filter.Transform;
            var mover = filter.Mover;

            pacman->Bombs--;

            var maze = PacmanStageMapData.Current(f).CurrentMazeData(f);
            FPVector2 target = maze.GhostHouse + FPVector2.Down * 6;
            FP travelTime = BombBaseTravelTime + BombDistanceTravelTime * FPVector2.Distance(target, transform->Position);
            mover->FreezeTime = travelTime;
            mover->Direction = -1;
            pacman->TemporaryInvincibility = travelTime;
            pacman->BombStartPosition = transform->Position;
            pacman->BombEndPosition = target;

            f.Events.PacmanUseBomb(entity, target);
            pacman->BombTravelTimer = travelTime;
            pacman->BombTravelTime = travelTime;
        }

        public void OnPowerPelletStart(Frame f, EntityRef entity) {
            //if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pacman)) {
            //    pacman->GhostCombo = 0;
            //}
            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                mover->SpeedMultiplier = 1;
            }

            var filter = f.Filter<PacmanPlayer, GridMover>();
            while (filter.NextUnsafe(out _, out var pacman2, out var mover2)) {
                if (!pacman2->HasPowerPellet) {
                    mover2->SpeedMultiplier = FP.FromString("0.85");
                }
            }
        }

        public void OnPowerPelletEnd(Frame f, EntityRef entity) {
            if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pacman)) {
                pacman->GhostCombo = 0;
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
                pac->RespawnTimer = 5;
            }

            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                mover->IsLocked = true;
            }
        }

        public void OnCharacterEaten(Frame f, EntityRef pacmanEntity, EntityRef other) {
            if (!f.Unsafe.TryGetPointer(pacmanEntity, out PacmanPlayer* pacman)) {
                return;
            }

            FP freeze = FP._0_33;
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

            // Bonus power pellet time
            pacman->PowerPelletTimer = FPMath.Min(pacman->PowerPelletTimer + freeze, pacman->PowerPelletFullTimer);

            // Freeze
            if (f.Unsafe.TryGetPointer(pacmanEntity, out GridMover* pacmanMover)) {
                pacman->TemporaryInvincibility = FP._0_10;
                pacmanMover->FreezeTime = freeze;
            }
            if (f.Unsafe.TryGetPointer(other, out GridMover* otherMover)) {
                otherMover->FreezeTime = freeze;
            }

            f.Signals.OnPacmanScored(pacmanEntity, points);
            f.Events.CharacterEaten(pacmanEntity, other, pacman->GhostCombo, points);
        }

        public void OnPacmanScored(Frame f, EntityRef pacmanEntity, int points) {
            if (!f.Unsafe.TryGetPointer(pacmanEntity, out PacmanPlayer* pacman)) {
                return;
            }

            pacman->RoundScore += points;

            RecalculateRankings(f);
            f.Events.PacmanScored(pacmanEntity, *pacman, points, pacman->RoundScore);
        }

        private static void RecalculateRankings(Frame f) {
            // TODO: is this slow? :(
            Dictionary<EntityRef, Ranking> thisRoundRankings = CalculateRankings(f, true);
            Dictionary<EntityRef, Ranking> entireGameRankings = CalculateRankings(f, false);

            foreach (var ranking in thisRoundRankings) {
                PacmanPlayer* pac = f.Unsafe.GetPointer<PacmanPlayer>(ranking.Key);
                pac->RoundRanking = thisRoundRankings[ranking.Key];
                pac->TotalRanking = entireGameRankings[ranking.Key];
            }
        }

        public static Dictionary<EntityRef, Ranking> CalculateRankings(Frame f, bool thisRoundOnly) {
            // TODO: is this slow? :(

            Dictionary<EntityRef, Ranking> rankings = new(4);
            List<EntityComponentPointerPair<PacmanPlayer>> players = new(4);

            foreach (var ecp in f.Unsafe.GetComponentBlockIterator<PacmanPlayer>()) {
                players.Add(ecp);
            }

            players.Sort((a, b) => b.Component->GetScore(thisRoundOnly) - a.Component->GetScore(thisRoundOnly));

            byte sharedRanking = 0;
            byte uniqueRanking = 0;
            int previousScore = players[0].Component->GetScore(thisRoundOnly);
            foreach (var player in players) {
                int score = player.Component->GetScore(thisRoundOnly);
                if (previousScore != score) {
                    // Different score, increment the ranking.
                    sharedRanking = uniqueRanking;
                    previousScore = score;
                }

                rankings.Add(player.Entity, new Ranking() {
                    SharedRanking = sharedRanking,
                    UniqueRanking = uniqueRanking++
                });
            }

            return rankings;
        }
    }
}
