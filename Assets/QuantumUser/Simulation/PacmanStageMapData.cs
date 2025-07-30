using System;
using Photon.Deterministic;

namespace Quantum {
    public unsafe class PacmanStageMapData : AssetObject {

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

        [Serializable]
        public class GhostPhase {
            public FP Timer;
            public bool IsScatter;
        }

        [Serializable]
        public class MazeData {
            public GhostPhase[] Phases;

            public SpawnPointData[] SpawnPoints;
            public FPVector2 Origin;
            public FPVector2 Size;
            public FPVector2 GhostHouse;
            public bool[] CollisionData;
            public byte[] PelletData;

            public FPVector2[] FruitSpawnPoints;
        }


        public AssetRef<EntityPrototype> PacmanPrototype;

        public MazeData[] Mazes;
        public FruitData[] FruitSpawnOrder;
        public AssetRef<EntityPrototype> FruitPrototype;
        public FPAnimationCurve BombHeightCurve;

        public MazeData CurrentMazeData(Frame f) {
            int index = f.Global->CurrentMazeIndex;
            if (index < 0 || index >= Mazes.Length) {
                return null;
            }

            return Mazes[index];
        }

        public static PacmanStageMapData Current(Frame f) {
            return f.FindAsset<PacmanStageMapData>(f.Map.UserAsset);
        }
    }
}
