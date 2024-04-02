using System;
using UnityEngine;

[Serializable] public struct ActionCost
{
    public int crops;
    public int lumber;
    public int stone;

    public bool CanBePerformed(float ownedCrops, float ownedLumber, float ownedStone)
    {
        return ownedCrops >= crops && ownedLumber >= lumber && ownedStone >= stone;
    }

    public bool TryPerform(ref float ownedCrops, ref float ownedLumber, ref float ownedStone)
    {
        if (!CanBePerformed(ownedCrops, ownedLumber, ownedStone)) {
            return false;
        }
        ownedCrops  -= crops;
        ownedLumber -= lumber;
        ownedStone  -= stone;
        return true;
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
    public ActionCost troopCavalier;
    public ActionCost troopGolem;
    
    public ActionCost GetBuilding(BuildingType type)
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
    
    public ActionCost GetTroop(TroopType type)
    {
        return type switch
        {
            TroopType.Knight   => troopKnight,
            TroopType.Archer   => troopArcher,
            TroopType.Cavalier => troopCavalier,
            TroopType.Golem    => troopGolem,
            _ => new ActionCost()
        };
    }
}
