namespace ExtendedItemTracking;

public interface ITrackItemRelationships
{
    public AIModule ModuleToTrackItemRelationship(CreatureTemplate.Relationship newRelationship);
    public ItemRelationshipTracker.TrackedItemState CreateTrackedItemState(ItemRelationshipTracker.DynamicItemRelationship itemRel);

    public CreatureTemplate.Relationship UpdateDynamicItemRelationship(ItemRelationshipTracker.DynamicItemRelationship dynamRelat);
}
