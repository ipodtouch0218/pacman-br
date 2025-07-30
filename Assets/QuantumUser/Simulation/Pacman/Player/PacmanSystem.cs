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
            var pacman = filter.Pacman;

            if (pacman->IsDead) {
                if (pacman->RespawnTimer > 0) {
                    if ((pacman->RespawnTimer -= f.DeltaTime) <= 0) {
                        pacman->RespawnTimer = 0;
                        f.Signals.OnPacmanRespawned(filter.Entity);
                    }
                }
            }

            // Full Invincibility
            if (pacman->Invincibility > 0) {
                if ((pacman->Invincibility -= f.DeltaTime) <= 0) {
                    pacman->Invincibility = 0;
                    f.Events.PacmanVulnerable(filter.Entity);
                }
            }

            if (pacman->IsDead) {
                return;
            }

            Input input = default;
            if (f.TryGet(filter.Entity, out PlayerLink pl)) {
                input = *f.GetPlayerInput(pl.Player);
            } else {
                // Bot input, potentially??
            }

            // Bombs
            if (pacman->BombTravelTimer > 0) {
                // Flying through the air. Gracefully.
                if ((pacman->BombTravelTimer -= f.DeltaTime) <= 0) {
                    pacman->BombTravelTimer = 0;
                    filter.Transform->Position = pacman->BombEndPosition;
                    f.Events.PacmanLandBombJump(filter.Entity);

                } else {
                    FP alpha = 1 - (pacman->BombTravelTimer / pacman->BombTravelTime);

                    FPVector2 newPos = FPVector2.Lerp(pacman->BombStartPosition, pacman->BombEndPosition, alpha);

                    var map = PacmanStageMapData.Current(f);
                    newPos += FPVector2.Up * map.BombHeightCurve.Evaluate(alpha);

                    filter.Transform->Position = newPos;
                    return;
                }
            } else if (pacman->Bombs > 0 && filter.Mover->FreezeTime <= 0) {
                // Check for bomb input
                if (input.Bomb.WasPressed) {
                    pacman->Bombs--;

                    var maze = PacmanStageMapData.Current(f).CurrentMazeData(f);
                    FPVector2 target = maze.GhostHouse + FPVector2.Down * 6;
                    FP travelTime = BombBaseTravelTime + BombDistanceTravelTime * FPVector2.Distance(target, filter.Transform->Position);
                    filter.Mover->FreezeTime = travelTime;
                    filter.Mover->Direction = -1;
                    pacman->TemporaryInvincibility = travelTime;
                    pacman->BombStartPosition = filter.Transform->Position;
                    pacman->BombEndPosition = target;

                    f.Events.PacmanUseBomb(filter.Entity, target);
                    pacman->BombTravelTimer = travelTime;
                    pacman->BombTravelTime = travelTime;
                    return;
                }
            }

            if (filter.Mover->FreezeTime > 0) {
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
                    if (!otherPac->HasPowerPellet && !otherPac->Invincible) {
                        // Pac1 eaten Pac2
                        f.Signals.OnCharacterEaten(filter.Entity, hit.Entity);
                        f.Signals.OnPacmanKilled(hit.Entity);
                    }
                }
            }
        }

        public void OnPowerPelletStart(Frame f, EntityRef entity) {
            //if (f.Unsafe.TryGetPointer(entity, out PacmanPlayer* pacman)) {
            //    pacman->GhostCombo = 0;
            //}
            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                mover->SpeedMultiplier = FP._1;
            }

            var filter = f.Filter<PacmanPlayer, GridMover>();
            while (filter.NextUnsafe(out _, out PacmanPlayer* pacman2, out GridMover* mover2)) {
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

                if (pac->HasPowerPellet) {
                    pac->PowerPelletTimer = 0;
                    f.Signals.OnPowerPelletEnd(entity);
                    f.Events.PowerPelletEnd(entity);
                }
            }

            if (f.Unsafe.TryGetPointer(entity, out GridMover* mover)) {
                mover->IsLocked = true;
            }

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

            // Bonus power pellet time
            pacman->PowerPelletTimer = FPMath.Min(pacman->PowerPelletTimer + FP._0_50, pacman->PowerPelletFullTimer);

            // Freeze
            if (f.Unsafe.TryGetPointer(pacmanEntity, out GridMover* pacmanMover)) {
                pacman->TemporaryInvincibility = FP._0_10;
                pacmanMover->FreezeTime = FP._0_50;
            }
            if (f.Unsafe.TryGetPointer(other, out GridMover* otherMover)) {
                otherMover->FreezeTime = FP._0_50;
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
