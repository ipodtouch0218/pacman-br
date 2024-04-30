namespace Quantum.Pacman.Freeze {
    public class FreezableSystemGroup : SystemMainThreadGroup {
        public FreezableSystemGroup(string name, params SystemMainThread[] children) : base(name, children) {
        }
    }
}
