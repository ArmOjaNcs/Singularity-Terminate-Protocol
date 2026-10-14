using System.Collections.Generic;
using Scellecs.Morpeh;

namespace ECS.PlayerComponents
{
    public struct PlayerInventoryComponent : IComponent
    {
        public Dictionary<string, int> ActiveUpgrades;
        public int CurrentWeaponsAmount;
        public int MaxWeapons;

        public int CurrentSkillsAmount;
        public int MaxSkills;

        public int CurrentTurretsAmount;
        public int MaxTurrets;
    }
}