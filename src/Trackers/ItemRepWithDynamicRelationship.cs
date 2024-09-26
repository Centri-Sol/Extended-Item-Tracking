namespace ExtendedItemTracking;

public class ItemRepWithDynamicRelationship : ItemTracker.ItemRepresentation
{
    public ItemRelationshipTracker.DynamicItemRelationship dynamicRelationship;

    public ItemRepWithDynamicRelationship(ItemTracker parent, AbstractPhysicalObject representedItem, float priority) :
        base(parent, representedItem, priority)
    {

    }
}
