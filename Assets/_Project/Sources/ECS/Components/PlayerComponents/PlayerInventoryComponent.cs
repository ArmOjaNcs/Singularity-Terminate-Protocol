using Scellecs.Morpeh;
using UniRx;

namespace ECS.PlayerComponents
{
    [System.Serializable]
    public struct PlayerInventoryComponent : IComponent
    {
        public ReactiveDictionary<string, int> ActiveSkills;
        public ReactiveDictionary<string, int> ActiveWeapons;
        public ReactiveDictionary<string, int> ActiveTurrets;
        public ReactiveProperty<int> MaxWeapons;
        public ReactiveProperty<int> MaxSkills;
        public ReactiveProperty<int> MaxTurrets;

        public int CurrentWeaponsAmount => ActiveWeapons.Count;

        public int CurrentTurretsAmount => ActiveTurrets.Count;

        public int CurrentItemsAmount => ActiveSkills.Count;
    }
}