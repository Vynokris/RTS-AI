using System;
using UnityEngine;

public enum BuildingType
{
    None,
    Castle,
    Farm,
    Lumbermill,
    Mine,
    Barracks,
}

public class Building : MonoBehaviour
{
    public int faction { get; private set; } = -1;
    public BuildingType type { get; private set; } = BuildingType.None;
    
    public void SetType(BuildingType buildingType) { type = buildingType; }
    
    public void SetFaction(int factionIdx)
    {
        faction = factionIdx;
    }

    private void Update()
    {
        switch (type)
        {
            case BuildingType.Farm:
                break;
            case BuildingType.Lumbermill:
                break;
            case BuildingType.Mine:
                break;
            case BuildingType.Barracks:
                break;
        }
    }
}
