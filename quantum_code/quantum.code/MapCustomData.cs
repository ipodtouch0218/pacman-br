using System;
using Photon.Deterministic;

namespace Quantum {
    public unsafe partial class MapCustomData {

        [Serializable]
        public struct SpawnPointData {
            public FPVector2 Position;
            public int Direction;
        }

        [Serializable]
        public struct FruitData {
            public int SpriteIndex;
            public int Points;
        }

        public SpawnPointData[] SpawnPoints;
        public FPVector2 MapOrigin;
        public FPVector2 MapSize;
        public FPVector2 GhostHouse;
        public bool[] CollisionData;
        public byte[] PelletData;

        public AssetRefEntityPrototype FruitPrototype;
        public FPVector2[] FruitSpawnPoints;
        public FruitData[] FruitSpawnOrder;
    }
}
