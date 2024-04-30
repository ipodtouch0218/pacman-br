using Photon.Deterministic;

namespace Quantum.Pacman.Fruit {
    public unsafe class FruitSystem : SystemSignalsOnly, ISignalOnPelletEat, ISignalOnGridMoverChangeTile {

        public void OnGridMoverChangeTile(Frame f, EntityRef entity, FPVector2 tile) {
            throw new System.NotImplementedException();
        }

        public void OnPelletEat(Frame f) {
            int remainingPellets = f.ResolveDictionary(f.Global->PelletData).Count;
        }
    }
}
