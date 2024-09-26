namespace ExtendedItemTracking;

public interface IUseModdedItemTracker : IUseItemTracker
{
    public void ItemSpotted(bool firstSpot, ItemTracker.ItemRepresentation itemRep);
}