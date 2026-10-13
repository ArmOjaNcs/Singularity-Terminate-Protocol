using System.Collections.Generic;
using Scellecs.Morpeh;

public struct PlayerInventoryComponent : IComponent
{
    public Dictionary<string, int> ActiveUpgrades;
}