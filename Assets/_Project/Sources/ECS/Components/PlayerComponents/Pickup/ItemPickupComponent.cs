using Scellecs.Morpeh;

public struct ItemPickupComponent : IComponent
{
    public string Id;
    public ItemTypes Type;
    public bool IsPickedUp;
}