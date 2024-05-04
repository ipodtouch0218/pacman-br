using System;
using Photon.Deterministic;

namespace Quantum {
    public unsafe partial class MapCustomData {

        [Serializable]
        public struct SpawnPointData {
            public FPVector2 Position;
            public int Direction;
        }

        public SpawnPointData[] SpawnPoints;
        public FPVector2 MapOrigin;
        public FPVector2 MapSize;
        public FPVector2 GhostHouse;
        public bool[] CollisionData;
        public byte[] PelletData;

        public FPVector2[] PillSpawnPoints;

    }
}
