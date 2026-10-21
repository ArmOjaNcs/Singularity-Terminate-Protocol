using Scellecs.Morpeh;
using UniRx;

namespace ECS.PlayerComponents
{
    [System.Serializable]
    public struct PlayerInventoryComponent : IComponent
    {
        public ReactiveDictionary<string, int> ActiveUpgrades;
        public int CurrentWeaponsAmount;
        public ReactiveProperty<int> MaxWeapons;

        public int CurrentItemsAmount;
        public ReactiveProperty<int> MaxItems;

        public int CurrentTurretsAmount;
        public ReactiveProperty<int> MaxTurrets;
    }
}