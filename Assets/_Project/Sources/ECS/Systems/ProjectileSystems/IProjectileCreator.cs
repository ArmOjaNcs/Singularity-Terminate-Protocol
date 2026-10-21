using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    public interface IProjectileCreator
    {
        void Create(Entity weapon, Entity[] projectiles);
    }
}