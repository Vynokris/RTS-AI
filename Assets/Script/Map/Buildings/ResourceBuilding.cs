using UnityEngine;

public class ResourceBuilding : Building
{
    [SerializeField] private float resourceProductionPerSecond = .2f;
    
    private void Update()
    {
        switch (owningTile.buildingType)
        {
            case BuildingType.Farm:
                owningTile.owningFaction.crops += resourceProductionPerSecond * Time.deltaTime;
                break;
            case BuildingType.Lumbermill:
                owningTile.owningFaction.lumber += resourceProductionPerSecond * Time.deltaTime;
                break;
            case BuildingType.Mine:
                owningTile.owningFaction.stone += resourceProductionPerSecond * Time.deltaTime;
                break;
        }
    }
}
