using Photon.Deterministic;
using System;

namespace Quantum.Util {
    public static class FPVectorUtils {

        public static FPVector2 Apply(FPVector2 input, Func<FP, FP> function) {
            return new FPVector2(function(input.X), function(input.Y));
        }

        public static FPVector2 WorldToCell(FPVector2 input, Frame f) {
            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);
            input -= mapdata.MapOrigin;
            return Apply(input, FPMath.Round);
        }

        public static FPVector2 CellToWorld(FPVector2 input, Frame f) {
            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);
            input += mapdata.MapOrigin;
            return Apply(input, FPMath.Round);
        }

        public static int CellToIndex(FPVector2 input, Frame f) {
            var mapdata = f.FindAsset<MapCustomData>(f.Map.UserAsset.Id);
            return input.X.AsInt + (input.Y.AsInt * mapdata.MapSize.X.AsInt);
        }

        public static int WorldToIndex(FPVector2 input, Frame f) {
            return CellToIndex(WorldToCell(input, f), f);
        }

        public static FPVector2 RoundToInt(FPVector2 input) {
            return new(FPMath.Round(input.X), FPMath.Round(input.Y));
        }
    }
}
