namespace Quantum.Pacman {
    public class PausableSystemGroup : SystemGroup {
        public override bool StartEnabled => false;
        public PausableSystemGroup(string name, params SystemBase[] children) : base(name, children) {

        }
    }
}
