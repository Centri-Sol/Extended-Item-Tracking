namespace ExtendedItemTracking;

public static class CreatureTemplateExtensions
{
    public static CreatureTemplate.Relationship[,] ItemRelationships = new CreatureTemplate.Relationship[ExtEnum<CreatureTemplate.Type>.values.Count, ExtEnum<AbstractPhysicalObject.AbstractObjectType>.values.Count];
    public static CreatureTemplate.Relationship ItemRelationship(this CreatureTemplate temp, AbstractPhysicalObject.AbstractObjectType objType) => ItemRelationships[temp.index, objType.index];

    public static void EstablishItemRelationship(CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, CreatureTemplate.Relationship relationship)
    {
        CreatureTemplate ctrTemp = StaticWorld.GetCreatureTemplate(ctrType);

        if (ctrType is null ||
            ctrType.Index == -1 ||
            itemType is null ||
            itemType.Index == -1)
        {
            return;
        }

        ItemRelationships[ctrType.Index, itemType.Index] = relationship;

        foreach (CreatureTemplate otherCtrTemp in StaticWorld.creatureTemplates)
        {
            if (otherCtrTemp is not null &&
                otherCtrTemp.ancestor == ctrTemp)
            {
                EstablishItemRelationship(otherCtrTemp.type, itemType, relationship);
            }
        }
    }
    public static void DoesntTrackItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.DoesntTrack, intensity));
    public static void IgnoresItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Ignores, intensity));
    public static void EatsItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Eats, intensity));
    public static void AttacksItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Attacks, intensity));
    public static void AntagonizesItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Antagonizes, intensity));
    public static void RivalsItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.AgressiveRival, intensity));
    public static void UncomfortableWithItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Uncomfortable, intensity));
    public static void StaysAwayFromItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.StayOutOfWay, intensity));
    public static void FearsItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Afraid, intensity));
    public static void PlaysWithItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.PlaysWith, intensity));
    public static void TreatsItemAsPack(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.Pack, intensity));
    public static void SocialDependentWithItem(this CreatureTemplate.Type ctrType, AbstractPhysicalObject.AbstractObjectType itemType, float intensity) => EstablishItemRelationship(ctrType, itemType, new(CreatureTemplate.Relationship.Type.SocialDependent, intensity));
}