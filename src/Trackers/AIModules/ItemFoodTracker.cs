namespace ExtendedItemTracking;

public class ItemFoodTracker : AIModule
{
    public class TrackedFood
    {
        public ItemFoodTracker owner;
        public ItemTracker.ItemRepresentation itemRep;

        public float EstimatedChanceOfFinding
        {
            get
            {
                float lastSeenFac = (10 + itemRep.TicksSinceSeen) / 4f;
                if (itemRep.VisualContact)
                {
                    return 1;
                }
                if (lastSeenFac < 45f)
                {
                    float findChance = (lastSeenFac / 12f) - 5f;
                    findChance = 1 + Mathf.Pow(2.71828175f, -findChance);
                    return Mathf.Clamp((1 / -findChance) + 1.007f, 0, 1);
                }
                return 1 / (lastSeenFac - 10f);
            }
        }
        public float Reachability
        {
            get
            {
                if (itemRep.BestGuessForPosition().room == owner.AI.creature.pos.room &&
                    owner.AI.creature.Room.realizedRoom is not null &&
                    owner.AI.creature.Room.realizedRoom.GetTile(itemRep.BestGuessForPosition()).Solid)
                {
                    return 0;
                }
                float reachability = owner.giveUpOnUnreachablePrey < 0 ? 1 : Mathf.InverseLerp(owner.giveUpOnUnreachablePrey, 0, unreachableCounter) * Mathf.InverseLerp(200, 100, atPositionButCantSeeCounter);
                if (owner.AI.creature.world.GetAbstractRoom(itemRep.BestGuessForPosition()).AttractionValueForCreature(owner.AI.creature.creatureTemplate.type) < owner.AI.creature.world.GetAbstractRoom(owner.AI.creature.pos).AttractionValueForCreature(owner.AI.creature.creatureTemplate.type))
                {
                    reachability *= 0.5f;
                }
                return reachability;
            }
        }
        public int unreachableCounter;
        public int atPositionButCantSeeCounter;
        public WorldCoordinate lastBestGuessPos;


        private float intensity;
        public float CurrentIntensity
        {
            get
            {
                if (itemRep.deleteMeNextFrame)
                {
                    return 0f;
                }
                return intensity;
            }
        }

        public TrackedFood(ItemFoodTracker owner, ItemTracker.ItemRepresentation itemRep)
        {
            this.owner = owner;
            this.itemRep = itemRep;
            intensity = owner.AI.DynamicItemRelationship(itemRep.representedItem).intensity / 2f;
            if (owner.AI.DynamicItemRelationship(itemRep.representedItem).type == CreatureTemplate.Relationship.Type.Eats)
            {
                intensity += 0.5f;
            }
        }

        public void Update()
        {
            if (itemRep.BestGuessForPosition().room == owner.AI.creature.pos.room)
            {
                intensity = owner.AI.DynamicItemRelationship(itemRep.representedItem).intensity / 2f;
                if (owner.AI.DynamicItemRelationship(itemRep.representedItem).type == CreatureTemplate.Relationship.Type.Eats)
                {
                    intensity += 0.5f;
                }
                return;
            }
            intensity = 0;
            int exitIndex = owner.AI.creature.Room.ExitIndex(itemRep.BestGuessForPosition().room);
            if (exitIndex > -1)
            {
                intensity = CurrentIntensity * 0.5f;
            }

            if (owner.AI.pathFinder is not null &&
                owner.AI.pathFinder.DoneMappingAccessibility)
            {
                WorldCoordinate guessPos = itemRep.BestGuessForPosition();
                bool reachable = owner.AI.pathFinder.CoordinateReachableAndGetbackable(guessPos);
                if (!reachable)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (reachable)
                        {
                            break;
                        }
                        reachable = owner.AI.pathFinder.CoordinateReachableAndGetbackable(guessPos + Custom.fourDirections[i]);
                    }
                }
                if (reachable)
                {
                    unreachableCounter = 0;
                }
                else
                {
                    unreachableCounter++;
                }
            }
            if (lastBestGuessPos == itemRep.BestGuessForPosition() &&
                owner.AI.creature.pos.room == itemRep.BestGuessForPosition().room &&
                owner.AI.creature.pos.Tile.FloatDist(itemRep.BestGuessForPosition().Tile) < 5f &&
                owner.AI.pathFinder is not null && owner.AI.pathFinder.GetDestination.room == itemRep.BestGuessForPosition().room &&
                owner.AI.pathFinder.GetDestination.Tile.FloatDist(itemRep.BestGuessForPosition().Tile) < 5f &&
                owner.AI.creature.Room.realizedRoom is not null && owner.AI.creature.Room.realizedRoom.VisualContact(owner.AI.creature.pos, itemRep.BestGuessForPosition()))
            {
                atPositionButCantSeeCounter += 5;
            }
            else
            {
                atPositionButCantSeeCounter--;
            }
            atPositionButCantSeeCounter = Custom.IntClamp(atPositionButCantSeeCounter, 0, 200);
            lastBestGuessPos = itemRep.BestGuessForPosition();
        }

        public bool PathFinderCanGetToPrey()
        {
            PathFinder pf = owner.AI.pathFinder;
            WorldCoordinate gp = itemRep.BestGuessForPosition();
            for (int i = 0; i < 9; i++)
            {
                if (pf.CoordinateReachable(WorldCoordinate.AddIntVector(gp, Custom.eightDirectionsAndZero[i])) &&
                    pf.CoordinatePossibleToGetBackFrom(WorldCoordinate.AddIntVector(gp, Custom.eightDirectionsAndZero[i])))
                {
                    return true;
                }
            }
            for (int j = 0; j < 4; j++)
            {
                if (pf.CoordinateReachable(WorldCoordinate.AddIntVector(gp, Custom.fourDirections[j] * 2)) &&
                    pf.CoordinatePossibleToGetBackFrom(WorldCoordinate.AddIntVector(gp, Custom.fourDirections[j] * 2)))
                {
                    return true;
                }
            }
            return false;
        }

        public float Attractiveness()
        {
            AbstractCreature ctr = owner.AI.creature;
            float intensity = owner.AI.DynamicItemRelationship(itemRep.representedItem).intensity;
            WorldCoordinate worldCoordinate = itemRep.BestGuessForPosition();
            float distEst = owner.DistanceEstimation(ctr.pos, worldCoordinate);
            distEst = Mathf.Pow(distEst, 1.5f);
            distEst = Mathf.Lerp(distEst, 1f, 0.5f);
            if (owner.AI.pathFinder is not null)
            {
                if (!owner.AI.pathFinder.CoordinateReachable(worldCoordinate))
                {
                    intensity /= 2f;
                }
                if (!owner.AI.pathFinder.CoordinatePossibleToGetBackFrom(worldCoordinate))
                {
                    intensity /= 2f;
                }
                if (!PathFinderCanGetToPrey())
                {
                    intensity /= 2f;
                }
            }
            intensity *= EstimatedChanceOfFinding;
            intensity *= Reachability;
            if (itemRep.representedItem.realizedObject is not null &&
                itemRep.representedItem.realizedObject.grabbedBy.Count > 0)
            {
                intensity = ctr.creatureTemplate.TopAncestor() != itemRep.representedItem.realizedObject.grabbedBy[0].grabber.abstractCreature.creatureTemplate.TopAncestor() ?
                    intensity * ctr.creatureTemplate.interestInOtherCreaturesCatches :
                    intensity * ctr.creatureTemplate.interestInOtherAncestorsCatches;
            }
            if (worldCoordinate.room != ctr.pos.room)
            {
                intensity *= Mathf.InverseLerp(0, 0.5f, ctr.world.GetAbstractRoom(worldCoordinate).AttractionValueForCreature(ctr.creatureTemplate.type));
            }
            intensity /= distEst;
            if (ctr.Room.world.game.IsStorySession &&
                itemRep.representedItem.type == AbstractPhysicalObject.AbstractObjectType.DataPearl)
            {
                intensity /= 10f;
            }
            return intensity;
        }
    }

    public ITrackItemRelationships owner => AI as ITrackItemRelationships;
    public List<TrackedFood> food;
    public TrackedFood currentItem;

    public int maxRememberedItems;
    public float persistanceBias;

    public float sureToGetFoodDistance;
    public float sureToLoseFoodDistance;

    public float frustration;
    public float frustrationSpeed;

    public int giveUpOnUnreachablePrey = 400;

    public AImap aimap => AI.creature.realizedCreature.room?.aimap;

    public int TotalTrackedFood => food.Count;

    public ItemTracker.ItemRepresentation MostAttractiveItem => currentItem?.itemRep;

    public ItemTracker.ItemRepresentation GetTrackedFood(int index) => food[index].itemRep;

    public override float Utility()
    {
        if (currentItem is null)
        {
            return 0;
        }
        AbstractCreature ctr = AI.creature;
        WorldCoordinate guessPos = currentItem.itemRep.BestGuessForPosition();
        if (ctr.abstractAI.WantToMigrate &&
            guessPos.room != ctr.abstractAI.MigrationDestination.room &&
            guessPos.room != ctr.pos.room)
        {
            return 0;
        }
        float utility = DistanceEstimation(ctr.pos, guessPos, ctr.creatureTemplate);
        utility = Mathf.Lerp(Mathf.InverseLerp(sureToLoseFoodDistance, sureToGetFoodDistance, utility), Mathf.Lerp(sureToGetFoodDistance, sureToLoseFoodDistance, 0.25f) / utility, 0.5f);
        float intensity = Mathf.Pow(currentItem.CurrentIntensity, 0.75f);
        utility *= intensity;
        utility = Mathf.Min(utility, intensity * currentItem.Reachability);
        return utility;
    }

    public ItemFoodTracker(ArtificialIntelligence AI, int maxRememberedItems, float persistanceBias, float sureToGetFoodDistance, float sureToLoseFoodDistance, float frustrationSpeed)
        : base(AI)
    {
        this.maxRememberedItems = maxRememberedItems;
        this.persistanceBias = persistanceBias;
        this.sureToGetFoodDistance = sureToGetFoodDistance;
        this.sureToLoseFoodDistance = sureToLoseFoodDistance;
        this.frustrationSpeed = frustrationSpeed;
        food = new List<TrackedFood>();
    }

    public override void Update()
    {
        float attracToBeat = float.MinValue;
        TrackedFood trackedPrey = null;
        for (int i = food.Count - 1; i >= 0; i--)
        {
            food[i].Update();
            float itemAttrac = food[i].Attractiveness();
            food[i].itemRep.forgetCounter = 0;
            if (food[i] == currentItem)
            {
                itemAttrac *= persistanceBias;
            }

            if (food[i].itemRep.deleteMeNextFrame)
            {
                food.RemoveAt(i);
            }
            else if (itemAttrac > attracToBeat)
            {
                attracToBeat = itemAttrac;
                trackedPrey = food[i];
            }
        }
        currentItem = trackedPrey;

        if (frustrationSpeed > 0 &&
            currentItem is not null &&
            AI.pathFinder is not null &&
            AI.creature.pos.room == currentItem.itemRep.BestGuessForPosition().room &&
            !currentItem.PathFinderCanGetToPrey())
        {
            frustration = Mathf.Clamp01(frustration + frustrationSpeed);
        }
        else
        {
            frustration = Mathf.Clamp01(frustration - frustrationSpeed * 4f);
        }

    }

    public virtual void AddFoodItem(ItemTracker.ItemRepresentation newItem)
    {
        foreach (TrackedFood item in food)
        {
            if (item.itemRep == newItem)
            {
                return;
            }
        }
        food.Add(new TrackedFood(this, newItem));
        if (food.Count > maxRememberedItems)
        {
            float lowestAttrac = float.MaxValue;
            TrackedFood trackedPrey = null;
            foreach (TrackedFood item2 in food)
            {
                if (item2.Attractiveness() < lowestAttrac)
                {
                    lowestAttrac = item2.Attractiveness();
                    trackedPrey = item2;
                }
            }
            trackedPrey.itemRep.Destroy();
            food.Remove(trackedPrey);
        }
        Update();
    }
    public virtual void ForgetFoodItem(AbstractPhysicalObject item)
    {
        for (int i = food.Count - 1; i >= 0; i--)
        {
            if (food[i].itemRep.representedItem == item)
            {
                food.RemoveAt(i);
            }
        }
    }
    public virtual void ForgetAllFood()
    {
        food.Clear();
        currentItem = null;
    }

    public virtual float DistanceEstimation(WorldCoordinate from, WorldCoordinate to, CreatureTemplate crit = null)
    {
        if (crit is not null &&
            from.room != to.room)
        {
            if (AI.creature.world.GetAbstractRoom(from).realizedRoom is not null &&
                AI.creature.world.GetAbstractRoom(from).realizedRoom.readyForAI &&
                AI.creature.world.GetAbstractRoom(from).ExitIndex(to.room) > -1)
            {
                int creatureSpecificExitIndex = AI.creature.world.GetAbstractRoom(from).CommonToCreatureSpecificNodeIndex(AI.creature.world.GetAbstractRoom(from).ExitIndex(to.room), crit);
                int exitDist = AI.creature.world.GetAbstractRoom(from).realizedRoom.aimap.ExitDistanceForCreatureAndCheckNeighbours(from.Tile, creatureSpecificExitIndex, crit);

                if (exitDist > -1 &&
                    crit.ConnectionResistance(MovementConnection.MovementType.SkyHighway).Allowed &&
                    AI.creature.world.GetAbstractRoom(from).AnySkyAccess &&
                    AI.creature.world.GetAbstractRoom(to).AnySkyAccess)
                {
                    exitDist = Math.Min(exitDist, 50);
                }
                if (exitDist > -1)
                {
                    return exitDist;
                }
            }
            return 50f;
        }
        return Vector2.Distance(IntVector2.ToVector2(from.Tile), IntVector2.ToVector2(to.Tile));
    }
}