namespace Quantum {
    public unsafe class GameplaySystemGroup : SystemGroup {
        public override bool StartEnabled => false;

        public GameplaySystemGroup(params SystemBase[] children) : base("gameplay", children) { }
    }
}