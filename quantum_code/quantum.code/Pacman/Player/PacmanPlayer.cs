namespace Quantum {
    partial struct PacmanPlayer {
        public bool HasPowerPellet => PowerPelletTimer > 0;
        public bool Invincible => Invincibility > 0 || TemporaryInvincibility > 0;

        public readonly int GetScore(bool thisRoundOnly) {
            if (thisRoundOnly) {
                return RoundScore;
            } else {
                return RoundScore + TotalScore;
            }
        }
    }
}
