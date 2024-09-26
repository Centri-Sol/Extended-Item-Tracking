namespace ExtendedItemTracking;

public class ItemRelationshipTracker : AIModule
{
    public class DynamicItemRelationship
    {
        public ItemRelationshipTracker rt;

        public ItemRepWithDynamicRelationship trackerRep;

        public CreatureTemplate.Relationship currentRelationship;

        public AIModule trackedByModule;

        public float trackedByModuleWeigth;

        public TrackedItemState state;

        public DynamicItemRelationship(ItemRelationshipTracker rt, ItemRepWithDynamicRelationship trackerRep, CreatureTemplate.Relationship initialRelationship)
        {
            this.rt = rt;
            this.trackerRep = trackerRep;
            _ = rt.visualize;
            currentRelationship = initialRelationship;
            rt.SortCreatureIntoModule(this, initialRelationship);
            state = (rt.AI as ITrackItemRelationships).CreateTrackedItemState(this);
            trackerRep.dynamicRelationship = this;
            trackedByModuleWeigth = 1f;
        }

        public void Update()
        {
            CreatureTemplate.Relationship newRelationship = (rt.AI as ITrackItemRelationships).UpdateDynamicItemRelationship(this);
            if (newRelationship.type != currentRelationship.type)
            {
                rt.SortCreatureIntoModule(this, newRelationship);
            }
            trackerRep.priority = newRelationship.intensity * trackedByModuleWeigth;
            currentRelationship = newRelationship;
        }
    }

    public class TrackedItemState
    {
        public bool held;
    }

    public class ItemRelationshipVisualizer
    {
        public class RelVis
        {
            public DynamicItemRelationship relationship;

            public FLabel txt;

            public FSprite line;

            public RelVis(DynamicItemRelationship relationship)
            {
                this.relationship = relationship;
                txt = new FLabel(Custom.GetFont(), "");
                Futile.stage.AddChild(txt);
                line = new FSprite("pixel");
                Futile.stage.AddChild(line);
                line.anchorY = 0f;
            }

            public void UpdateGraphics(Vector2 dispPos)
            {
                txt.x = dispPos.x;
                txt.y = dispPos.y;
                dispPos.y -= 8f;
                line.x = dispPos.x;
                line.y = dispPos.y;
                Vector2 val = relationship.rt.AI.creature.realizedCreature.mainBodyChunk.pos - relationship.rt.AI.creature.realizedCreature.room.game.cameras[0].pos;
                line.scaleY = Vector2.Distance(dispPos, val);
                line.rotation = Custom.AimFromOneVectorToAnother(dispPos, val);
                txt.text = $"{relationship.currentRelationship.type}|{relationship.currentRelationship.intensity}|{(relationship.trackedByModule is not null ? $"Tracked By: {relationship.trackedByModule}" : "~")}";
                txt.color = RelationshipColor(relationship.currentRelationship.type);
                line.color = RelationshipColor(relationship.currentRelationship.type);
            }

            public void ClearSprites()
            {
                txt.RemoveFromContainer();
                line.RemoveFromContainer();
            }
        }

        public ItemRelationshipTracker rt;

        public List<RelVis> relVises;

        public FLabel itemTrackerText;

        public FLabel foodItemText;

        public FLabel itemThreatsText;

        public static Color RelationshipColor(CreatureTemplate.Relationship.Type tp)
        {
            if (tp == CreatureTemplate.Relationship.Type.Eats)
            {
                return new HSLColor(140/360f, .75f, .5f).rgb;
            }
            if (tp == CreatureTemplate.Relationship.Type.Afraid)
            {
                return new HSLColor(250/360f, .6f, .5f).rgb;
            }
            if (tp == CreatureTemplate.Relationship.Type.Uncomfortable)
            {
                return new HSLColor(40/360f, .75f, .5f).rgb;
            }
            if (tp == CreatureTemplate.Relationship.Type.Antagonizes)
            {
                return new HSLColor(0, .75f, .5f).rgb;
            }
            if (tp == CreatureTemplate.Relationship.Type.AgressiveRival)
            {
                return new HSLColor(20/360f, 1, .5f).rgb;
            }
            if (tp == CreatureTemplate.Relationship.Type.PlaysWith)
            {
                return new HSLColor(350/360f, .5f, .5f).rgb;
            }
            if (tp == CreatureTemplate.Relationship.Type.StayOutOfWay)
            {
                return new HSLColor(.5f, 1, .3f).rgb;
            }
            return new HSLColor(1, 1, .85f).rgb;
        }

        public ItemRelationshipVisualizer(ItemRelationshipTracker rt)
        {
            this.rt = rt;
            relVises = new List<RelVis>();
            itemTrackerText = new FLabel(Custom.GetFont(), "");
            foodItemText = new FLabel(Custom.GetFont(), "");
            itemThreatsText = new FLabel(Custom.GetFont(), "");
            //aggressionText = new FLabel(Custom.GetFont(), "");
            foodItemText.color = RelationshipColor(CreatureTemplate.Relationship.Type.Eats);
            itemThreatsText.color = RelationshipColor(CreatureTemplate.Relationship.Type.Afraid);
            //aggressionText.color = RelationshipColor(CreatureTemplate.Relationship.Type.AgressiveRival);
            Futile.stage.AddChild(itemTrackerText);
            Futile.stage.AddChild(foodItemText);
            Futile.stage.AddChild(itemThreatsText);
            //Futile.stage.AddChild(aggressionText);
            for (int i = 0; i < rt.relationships.Count; i++)
            {
                NewRel(rt.relationships[i]);
            }
        }

        public void NewRel(DynamicItemRelationship rel)
        {
            relVises.Add(new RelVis(rel));
        }

        public void Update()
        {
            Vector2 ownerPos = rt.AI.creature.realizedCreature.mainBodyChunk.pos - rt.AI.creature.realizedCreature.room.game.cameras[0].pos;
            itemTrackerText.x = ownerPos.x + 20f;
            itemTrackerText.y = ownerPos.y + 120f;
            foodItemText.x = ownerPos.x + 20f;
            foodItemText.y = ownerPos.y + 100f;
            itemThreatsText.x = ownerPos.x + 20f;
            itemThreatsText.y = ownerPos.y + 80f;
            //aggressionText.x = ownerPos.x + 20f;
            //aggressionText.y = ownerPos.y + 60f;
            itemTrackerText.text = "Total items tracked: " + rt.itemTracker.ItemCount;
            ItemFoodTracker ift = rt.AI.ItemFoodTracker();
            ItemThreatTracker itt = rt.AI.ItemThreatTracker();
            foodItemText.text = ift is not null ? $"Food items tracked: {ift.TotalTrackedFood}" : "No ItemFoodTracker";
            itemThreatsText.text = itt is not null ? $"Threat items tracked:{itt.TotalTrackedThreats} (Items: {itt.TotalTrackedThreatItems})" : "No ItemThreatTracker";
            //aggressionText.text = (rt.AI.agressionTracker is not null) ? ("Aggression Targets tracked: " + rt.AI.agressionTracker.TotalTrackedCreatures) : "No AgressionTracker";

            int num = 0;
            for (int i = 0; i < relVises.Count; i++)
            {
                Vector2 dispPos;
                if (relVises[i].relationship.trackerRep.representedItem.realizedObject is not null &&
                    relVises[i].relationship.trackerRep.representedItem.realizedObject.room == rt.AI.creature.realizedCreature.room)
                {
                    PhysicalObject obj = relVises[i].relationship.trackerRep.representedItem.realizedObject;
                    dispPos = obj.bodyChunks[obj.bodyChunks.Length/2].pos + new Vector2(0, 30f) - rt.AI.creature.realizedCreature.room.game.cameras[0].pos;
                }
                else
                {
                    dispPos = ownerPos + new Vector2(160f, 30f - 20f * num);
                    num++;
                }
                relVises[i].UpdateGraphics(dispPos);
            }
        }

        public void ClearAll()
        {
            for (int num = relVises.Count - 1; num >= 0; num--)
            {
                relVises[num].ClearSprites();
                relVises.RemoveAt(num);
            }
        }

        public void ClearSpecific(DynamicItemRelationship relationship)
        {
            for (int num = relVises.Count - 1; num >= 0; num--)
            {
                if (relVises[num].relationship == relationship)
                {
                    relVises[num].ClearSprites();
                    relVises.RemoveAt(num);
                }
            }
        }
    }


    public ItemTracker itemTracker;

    public bool visualize;

    private ItemRelationshipVisualizer viz;

    public List<DynamicItemRelationship> relationships;

    private bool ignoreModuleAbandon;

    public ItemRelationshipTracker(ArtificialIntelligence AI, ItemTracker it) : base(AI)
    {
        itemTracker = it;
        relationships = new();
    }


    public override void Update()
    {
        if (visualize)
        {
            if (viz is null)
            {
                viz = new ItemRelationshipVisualizer(this);
            }
            else
            {
                viz.Update();
            }
        }
        else if (viz is not null)
        {
            viz.ClearAll();
            viz = null;
        }
        for (int t = relationships.Count - 1; t >= 0; t--)
        {
            if (relationships[t].trackerRep.deleteMeNextFrame)
            {
                if (visualize)
                {
                    viz.ClearSpecific(relationships[t]);
                }
                relationships.RemoveAt(t);
            }
            else
            {
                relationships[t].Update();
            }
        }
    }

    public void EstablishDynamicRelationship(ItemRepWithDynamicRelationship item)
    {
        _ = visualize;
        CreatureTemplate.Relationship initialRelationship = AI.StaticItemRelationship(item.representedItem);
        if (initialRelationship.type == CreatureTemplate.Relationship.Type.DoesntTrack)
        {
            return;
        }
        for (int i = 0; i < relationships.Count; i++)
        {
            if (relationships[i].trackerRep == item)
            {
                return;
            }
        }
        DynamicItemRelationship dynamicRelationship = new(this, item, initialRelationship);
        relationships.Add(dynamicRelationship);
        if (viz is not null)
        {
            viz.NewRel(dynamicRelationship);
        }
    }

    private void ForgetItemAndStopTracking(AbstractPhysicalObject item)
    {
        for (int i = 0; i < relationships.Count; i++)
        {
            if (relationships[i].trackerRep.representedItem == item)
            {
                relationships[i].trackerRep.Destroy();
                if (visualize)
                {
                    viz.ClearSpecific(relationships[i]);
                }
                relationships.RemoveAt(i);
            }
        }
    }

    public void ModuleHasAbandonedItem(ItemRepWithDynamicRelationship item, AIModule module)
    {
        if (ignoreModuleAbandon)
        {
            return;
        }
        _ = visualize;
        for (int i = 0; i < relationships.Count; i++)
        {
            if (relationships[i].trackerRep == item)
            {
                _ = visualize;
                ForgetItemAndStopTracking(item.representedItem);
                break;
            }
        }
    }

    public void SortCreatureIntoModule(DynamicItemRelationship relItem, CreatureTemplate.Relationship newRelationship)
    {
        _ = visualize;
        AIModule Tracker = (AI as ITrackItemRelationships).ModuleToTrackItemRelationship(newRelationship);
        if (relItem.trackedByModule == Tracker)
        {
            return;
        }

        if (relItem.trackedByModule is not null)
        {
            ignoreModuleAbandon = true;
            if (relItem.trackedByModule is ItemFoodTracker ift)
            {
                ift.ForgetFoodItem(relItem.trackerRep.representedItem);
            }
            else if (relItem.trackedByModule is ItemThreatTracker itt)
            {
                itt.RemoveThreatItem(relItem.trackerRep.representedItem);
            }
            ignoreModuleAbandon = false;
        }

        relItem.trackedByModule = Tracker;

        if (newRelationship.type == CreatureTemplate.Relationship.Type.DoesntTrack)
        {
            ForgetItemAndStopTracking(relItem.trackerRep.representedItem);
            _ = visualize;
        }
        else if (relItem.trackedByModule is not null)
        {
            if (relItem.trackedByModule is ItemFoodTracker ift)
            {
                ift.AddFoodItem(relItem.trackerRep);
                _ = visualize;
            }
            else if (relItem.trackedByModule is ItemThreatTracker itt)
            {
                itt.AddThreatItem(relItem.trackerRep);
                _ = visualize;
            }
        }
    }


}
