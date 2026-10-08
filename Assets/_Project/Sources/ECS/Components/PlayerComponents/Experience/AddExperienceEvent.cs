using Scellecs.Morpeh;

namespace ECS.PlayerSystems
{
    public struct AddExperienceEvent : IComponent
    {
        public int Amount;
        public Entity LooterEntity;
    }
}