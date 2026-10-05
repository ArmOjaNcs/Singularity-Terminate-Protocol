using Scellecs.Morpeh;

public struct ItemPickupComponent : IComponent
{
    public string Id;
    public ItemTypes Type;
    public int AddExpAmount;
    public bool IsPickedUp;
}