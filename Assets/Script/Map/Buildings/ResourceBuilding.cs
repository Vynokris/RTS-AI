using UnityEngine;

public class ResourceBuilding : Building
{
    [SerializeField] private float resourceProductionPerSecond = .2f;
    
    private void Update()
    {
        switch (owningTile.buildingType)
        {
            case BuildingType.Farm:
                owningTile.owningFaction.crops += resourceProductionPerSecond;
                break;
            case BuildingType.Lumbermill:
                owningTile.owningFaction.lumber += resourceProductionPerSecond;
                break;
            case BuildingType.Mine:
                owningTile.owningFaction.stone += resourceProductionPerSecond;
                break;
        }
    }
}
