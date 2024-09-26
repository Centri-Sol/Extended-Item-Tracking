using MonoMod.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace ExtendedItemTracking;

public static class Hooks
{
    public static void Apply()
    {
        ILHook_ItemTracker_ItemNoticed();
        On.ArtificialIntelligence.ctor += IntegrateItemTrackersToAI;
        On.ArtificialIntelligence.AddModule += AddExtendedItemTrackerModules;
        On.ItemTracker.ItemNoticed += IntegrateItemRelationshipTracker;
        On.AbstractCreature.OpportunityToEnterDen += StashItemsBroughtToDen;
        On.AbstractCreature.IsEnteringDen += StopCaringAboutItemsBroughtToDen;
    }

    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    public static void ILHook_ItemTracker_ItemNoticed()
    {
        IL.ItemTracker.ItemNoticed += IL =>
        {
            ILLabel? label = null;
            ILCursor c = new(IL);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<ItemTracker>(nameof(ItemTracker.items)),
                x => x.MatchCallvirt<List<ItemTracker.ItemRepresentation>>(nameof(List<ItemTracker.ItemRepresentation>.Count)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<ItemTracker>(nameof(ItemTracker.maxTrackedItems)),
                x => x.MatchBle(out label)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((ItemTracker it, AbstractPhysicalObject item) => it.AI is ITrackItemRelationships);
                c.Emit(OpCodes.Brtrue, label);
            }
            else
            {
                Debug.LogError("[ExtendedItemTracking] IL Hook to ItemTracker.ItemNoticed is having issues!");
            }
        };
    }
    public static void IntegrateItemTrackersToAI(On.ArtificialIntelligence.orig_ctor orig, ArtificialIntelligence AI, AbstractCreature ctr, World world)
    {
        if (AI is not null and ITrackItemRelationships itr &&
            !ItemTrackerExtensions.ItemTrackers.TryGetValue(itr, out _))
        {
            ItemTrackerExtensions.ItemTrackers.Add(itr, new ItemTrackerExtensions.ExtendedItemTrackingForAI());
        }
        orig(AI, ctr, world);
    }
    public static void AddExtendedItemTrackerModules(On.ArtificialIntelligence.orig_AddModule orig, ArtificialIntelligence AI, AIModule module)
    {
        orig(AI, module);
        if (!AI.UsesExtendedItemTracking(out var itrAI))
        {
            return;
        }

        if (module is ItemFoodTracker ift)
        {
            itrAI.itemFoodTracker = ift;
        }
        else if (module is ItemThreatTracker itt)
        {
            itrAI.itemThreatTracker = itt;
        }
        else if (module is ItemRelationshipTracker irt)
        {
            itrAI.itemRelationshipTracker = irt;
        }

    }
    public static ItemTracker.ItemRepresentation IntegrateItemRelationshipTracker(On.ItemTracker.orig_ItemNoticed orig, ItemTracker it, AbstractPhysicalObject item)
    {
        bool wasThereBefore = it.items.Contains(_ = new ItemTracker.ItemRepresentation(it, item, 0));
        ItemTracker.ItemRepresentation itemRep = orig(it, item);
        if (it?.AI is null or not ITrackItemRelationships)
        {
            return itemRep;
        }
        if (itemRep is null ||
            it.AI.StaticItemRelationship(item).type == CreatureTemplate.Relationship.Type.DoesntTrack)
        {
            if (item is not null)
            {
                ItemTracker.ItemRepresentation dupeRep = new(it, item, 0);
                if (!wasThereBefore &&
                    it.items.Contains(dupeRep))
                {
                    it.items.Remove(dupeRep);
                }
                _ = dupeRep;
            }
            return null;
        }

        for (int i = 0; i < it.items.Count; i++)
        {
            if (it.items[i].representedItem == item)
            {
                it.items[i] = itemRep = new ItemRepWithDynamicRelationship(itemRep.parent, itemRep.representedItem, itemRep.priority);
                break;
            }
        }

        bool oneIsCarryingOther = false;
        if (it.AI.creature.creatureTemplate.grasps > 0)
        {
            foreach (AbstractPhysicalObject.AbstractObjectStick stuckObject in it.AI.creature.stuckObjects)
            {
                if (stuckObject.A == item ||
                    stuckObject.B == item)
                {
                    oneIsCarryingOther = true;
                    break;
                }
            }
        }

        if (it.AI is IUseModdedItemTracker mit)
        {
            mit.ItemSpotted(!oneIsCarryingOther, itemRep);
        }

        if (it.AI.UsesExtendedItemTracking(out var itrAI) &&
            itrAI.itemRelationshipTracker is not null)
        {
            itrAI.itemRelationshipTracker.EstablishDynamicRelationship(itemRep as ItemRepWithDynamicRelationship);
        }

        if (it.items.Count > it.maxTrackedItems)
        {
            float intenToBeat = float.MaxValue;
            ItemTracker.ItemRepresentation otherRep = null;
            foreach (ItemTracker.ItemRepresentation itm in it.items)
            {
                float otherInten = it.AI.DynamicItemRelationship(itm).intensity;

                otherInten *= 100000f;
                otherInten += (itm.VisualContact ? 2 : 1) / (1 + itm.BestGuessForPosition().Tile.FloatDist(it.AI.creature.pos.Tile));
                otherInten /= Mathf.Lerp(itm.forgetCounter, 100, 0.7f);
                if (otherInten < intenToBeat)
                {
                    intenToBeat = otherInten;
                    otherRep = itm;
                }
            }
            if (otherRep == itemRep)
            {
                itemRep = null;
            }
            otherRep.Destroy();
        }

        return itemRep;
    }
    public static void StashItemsBroughtToDen(On.AbstractCreature.orig_OpportunityToEnterDen orig, AbstractCreature absCtr, WorldCoordinate den)
    {
        orig(absCtr, den);

        if (absCtr is null ||
            absCtr.InDen ||
            absCtr.creatureTemplate.doesNotUseDens ||
            !absCtr.creatureTemplate.stowFoodInDen)
        {
            return;
        }

        for (int i = 0; i < absCtr.stuckObjects.Count; i++)
        {
            if (absCtr.stuckObjects[i] is AbstractPhysicalObject.CreatureGripStick &&
                absCtr.stuckObjects[i].A == absCtr &&
                absCtr.stuckObjects[i].B is not AbstractCreature)
            {
                absCtr.remainInDenCounter = 200;
                absCtr.Room.MoveEntityToDen(absCtr);
                break;
            }
        }
    }
    public static void StopCaringAboutItemsBroughtToDen(On.AbstractCreature.orig_IsEnteringDen orig, AbstractCreature absCtr, WorldCoordinate den)
    {
        if (absCtr?.abstractAI?.RealAI?.ItemFoodTracker() is null)
        {
            orig(absCtr, den);
            return;
        }

        for (int o = absCtr.stuckObjects.Count - 1; o >= 0; o--)
        {
            if (absCtr.stuckObjects[o] is not AbstractPhysicalObject.CreatureGripStick ||
                absCtr.stuckObjects[o].A != absCtr ||
                absCtr.stuckObjects[o].B is null ||
                absCtr.stuckObjects[o].B is AbstractCreature)
            {
                continue;
            }
            absCtr.abstractAI.RealAI.ItemFoodTracker().ForgetFoodItem(absCtr.stuckObjects[o].B);
        }

        orig(absCtr, den);
    }

}