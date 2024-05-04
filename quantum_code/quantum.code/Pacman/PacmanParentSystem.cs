namespace Quantum.Pacman {
    public unsafe class PacmanParentSystem : SystemMainThreadFilter<PacmanParentSystem.Filter> {

        public struct Filter {
            public EntityRef Entity;
            public Transform2D* Transform;
            public PacmanParent* Parent;
        }

        public override void Update(Frame f, ref Filter filter) {
            if (f.TryGet(filter.Parent->Entity, out Transform2D parentTransform)) {
                filter.Transform->Position = parentTransform.Position;
            }
        }
    }
}
