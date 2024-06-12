namespace Quantum {
    partial struct PacmanPlayer {
        public bool HasPowerPellet => PowerPelletTimer > 0;
        public bool Invincible => Invincibility > 0 || TemporaryInvincibility > 0;
    }
}
