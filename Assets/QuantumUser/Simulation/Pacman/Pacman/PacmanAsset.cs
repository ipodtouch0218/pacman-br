using Photon.Deterministic;

namespace Quantum {
    public class PacmanAsset : AssetObject {

        public FP BombBaseTravelTime = FP.FromString("0.225");
        public FP BombDistanceTravelTime = FP.FromString("0.0075");
        public FP BombUseTimeAfterDeath = FP._0_10;
        public FP RespawnTime = 5;
        public FP RespawnInvincibilityTime = 5;
        public FP PowerPelletTime = FP.FromString("7.5");

        public FP OtherPacmanPowerPelletSlowdownMultiplier = FP.FromString("0.85");

        public FP GhostEatFreezeTime = FP._0_33;
        public FP GhostEatAdditionalInvincibilityTime = FP._0_10;

    }
}