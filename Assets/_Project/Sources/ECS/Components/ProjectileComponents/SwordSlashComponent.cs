using Scellecs.Morpeh;

namespace ECS.ProjectileComponents
{
    public struct SwordSlashComponent : IComponent
    {
        public int SequenceId;
        public int SwingIndex;
        public bool MirrorY;
    }
}
