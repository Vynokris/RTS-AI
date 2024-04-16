using System;
using UnityEngine;

[Serializable] public struct ActionCost
{
    public int crops;
    public int lumber;
    public int stone;

    public bool CanPerform(float ownedCrops, float ownedLumber, float ownedStone)
    {
        return ownedCrops >= crops && ownedLumber >= lumber && ownedStone >= stone;
    }

    public Vector3 RatioOwnedToNecessary(float ownedCrops, float ownedLumber, float ownedStone)
    {
        return new Vector3(crops  <= 0 ? 1 : crops  / ownedCrops, 
                           lumber <= 0 ? 1 : lumber / ownedLumber, 
                           stone  <= 0 ? 1 : stone  / ownedStone);
    }

    /// Should only ever be used if CanPerform returns true.
    public void ForcePerform(ref float ownedCrops, ref float ownedLumber, ref float ownedStone)
    {
        ownedCrops  -= crops;
        ownedLumber -= lumber;
        ownedStone  -= stone;
    }

    public bool TryPerform(ref float ownedCrops, ref float ownedLumber, ref float ownedStone)
    {
        if (!CanPerform(ownedCrops, ownedLumber, ownedStone)) {
            return false;
        }
        ForcePerform(ref ownedCrops, ref ownedLumber, ref ownedStone);
        return true;
    }

    public void Undo(ref float ownedCrops, ref float ownedLumber, ref float ownedStone)
    {
        ownedCrops  += crops;
        ownedLumber += lumber;
        ownedStone  += stone;
    }
}

public class CostStorage : MonoBehaviour
{
    [Header("Buildings")]
    public ActionCost buildingCastle;
    public ActionCost buildingBarracks;
    public ActionCost buildingFarm;
    public ActionCost buildingLumbermill;
    public ActionCost buildingMine;
    
    [Header("Troops")]
    public ActionCost troopKnight;
    public ActionCost troopArcher;
    public ActionCost troopGolem;
    
    public ActionCost GetBuildingCost(BuildingType type)
    {
        return type switch
        {
            BuildingType.Castle     => buildingCastle,
            BuildingType.Farm       => buildingFarm,
            BuildingType.Lumbermill => buildingLumbermill,
            BuildingType.Mine       => buildingMine,
            BuildingType.Barracks   => buildingBarracks,
            _ => new ActionCost()
        };
    }
    
    public ActionCost GetTroopCost(TroopType type)
    {
        return type switch
        {
            TroopType.Knight   => troopKnight,
            TroopType.Archer   => troopArcher,
            TroopType.Golem    => troopGolem,
            _ => new ActionCost()
        };
    }
}
