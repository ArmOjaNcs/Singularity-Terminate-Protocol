using Scellecs.Morpeh;
using UniRx;

namespace ECS.PlayerSystems
{
    public struct PlayerExperienceComponent : IComponent
    {
        public ReactiveProperty<int> CurrentXP;
        public int NextLevelXP;
        public int CurrentLevel;
    }
}