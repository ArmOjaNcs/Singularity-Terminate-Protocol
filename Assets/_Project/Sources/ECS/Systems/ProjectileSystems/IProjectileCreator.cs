using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    public interface IProjectileCreator
    {
        void Begin(Entity weapon); 
        void Update(Entity weapon, float deltaTime);
        void Cancel(Entity weapon);
    }
}