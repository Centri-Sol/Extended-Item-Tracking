namespace ExtendedItemTracking;

public static class ItemTrackerExtensions
{
    public static CreatureTemplate.Relationship StaticItemRelationship(this ArtificialIntelligence AI, AbstractPhysicalObject item)
    {
        if (item is null)
        {
            return new(CreatureTemplate.Relationship.Type.DoesntTrack, 0);
        }
        if (item is AbstractCreature absCtr)
        {
            return AI.StaticRelationship(absCtr);
        }
        return AI.creature.creatureTemplate.ItemRelationship(item.type);
    }

    public static void ForgetItem(this ItemTracker it, AbstractPhysicalObject item)
    {
        for (int i = it.items.Count - 1; i >= 0; i--)
        {
            AbstractPhysicalObject obj = it.items[i].representedItem;
            if (obj is not null &&
                obj == item)
            {
                it.items[i].Destroy();
                break;
            }
        }
    }
    public static void ForgetAllItems(this ItemTracker it) => it.items.Clear();
    public static ItemTracker.ItemRepresentation RepresentationForObject(this ItemTracker it, AbstractPhysicalObject obj, bool AddIfMissing)
    {
        ItemTracker.ItemRepresentation itemRep = null;
        foreach (ItemTracker.ItemRepresentation item in it.items)
        {
            if (item.representedItem == obj)
            {
                itemRep = item;
                break;
            }
        }
        if (itemRep is null && AddIfMissing)
        {
            itemRep = it.ItemNoticed(obj);
        }
        return itemRep;
    }


    public static ConditionalWeakTable<ITrackItemRelationships, ExtendedItemTrackingForAI> ItemTrackers = new();
    public static bool UsesExtendedItemTracking(this ArtificialIntelligence AI)
    {
        if (AI is not null and ITrackItemRelationships itr &&
            ItemTrackers.TryGetValue(itr, out _))
        {
            return true;
        }
        return false;
    }
    public static bool UsesExtendedItemTracking(this ArtificialIntelligence AI, out ExtendedItemTrackingForAI extTrckng)
    {
        if (AI is not null and ITrackItemRelationships itr &&
            ItemTrackers.TryGetValue(itr, out extTrckng))
        {
            return true;
        }
        extTrckng = null;
        return false;
    }


    public static ItemFoodTracker ItemFoodTracker(this ArtificialIntelligence AI) => AI.UsesExtendedItemTracking(out var itrAI) ? itrAI.itemFoodTracker : null;
    public static ItemThreatTracker ItemThreatTracker(this ArtificialIntelligence AI) => AI.UsesExtendedItemTracking(out var itrAI) ? itrAI.itemThreatTracker : null;
    public static ItemRelationshipTracker ItemRelationshipTracker(this ArtificialIntelligence AI) => AI.UsesExtendedItemTracking(out var itrAI) ? itrAI.itemRelationshipTracker : null;

    public static CreatureTemplate.Relationship DynamicItemRelationship(this ArtificialIntelligence AI, ItemTracker.ItemRepresentation rep) => AI.DynamicItemRelationship(rep, null);
    public static CreatureTemplate.Relationship DynamicItemRelationship(this ArtificialIntelligence AI, AbstractPhysicalObject absObj) => AI.DynamicItemRelationship(null, absObj);
    public static CreatureTemplate.Relationship DynamicItemRelationship(this ArtificialIntelligence AI, ItemTracker.ItemRepresentation rep, AbstractPhysicalObject absObj)
    {
        if (rep is null)
        {
            rep = AI.itemTracker.RepresentationForObject(absObj, false);
        }
        if (rep is null)
        {
            return AI.StaticItemRelationship(absObj);
        }
        if (rep is ItemRepWithDynamicRelationship relRep &&
            relRep.dynamicRelationship is not null)
        {
            return relRep.dynamicRelationship.currentRelationship;
        }
        return AI.StaticItemRelationship(rep.representedItem);
    }

    public static CreatureTemplate.Relationship ObjectRelationship(this ArtificialIntelligence AI, AbstractPhysicalObject absObj)
    {
        if (absObj is AbstractCreature absCtr)
        {
            return AI.DynamicRelationship(absCtr);
        }
        return AI.DynamicItemRelationship(null, absObj);
    }

    public class ExtendedItemTrackingForAI
    {
        public ItemFoodTracker itemFoodTracker;

        public ItemThreatTracker itemThreatTracker;

        public ItemRelationshipTracker itemRelationshipTracker;

        public ExtendedItemTrackingForAI()
        {

        }
    }
}