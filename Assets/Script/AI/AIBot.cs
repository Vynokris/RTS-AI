using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class AIBot : Faction
{
    public class UtilityData
    {
        public Building buildingToRepair = null;
        public Building buildingToGuard  = null;
    }
    
    private UtilitySystem utilitySystem;
    private UtilityData   utilityData;

    private Thread thread;
    private bool isStopped;

    public override void Awake()
    {
        base.Awake();
        thread = new Thread(Run);
        
        utilitySystem = GetComponent<UtilitySystem>();
        utilitySystem.functionCallerType   = typeof(AIBot);
        utilitySystem.functionCallerScript = this;
    }

    private void OnDestroy()
    {
        thread.Abort();
        isStopped = true;
    }
    private void OnApplicationQuit()
    {
        thread.Abort();
        isStopped = true;
    }

    void Run()
    {
        while (!isStopped)
        {
            utilitySystem.PerformBestAction();
            Thread.Sleep(1000);
        }
    }
    
    #region EvaluationMethods
    
    public float PlaceBuildingNecessity()
    {
        return 0;
    }
    
    public float RepairBuildingNecessity()
    {
        float lowestRatio = 1;
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None) continue;
            float healthRatio = tile.building.health / tile.building.maxHealth;
            if (lowestRatio > healthRatio) {
                lowestRatio = healthRatio;
                utilityData.buildingToRepair = tile.building;
            }
        }
        return lowestRatio;
    }
    
    public float GuardBuildingNecessity()
    {
        float maxInfluence = 0;
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None) continue; // TODO: continue if the building is already guarded.
            float playerBuildingsInfluence = influenceManager.GetInfluence(tile.transform.position, FactionManager.playerFaction.GetID(), InfluenceType.Buildings);
            float resourcesInfluence       = influenceManager.GetInfluence(tile.transform.position, Faction.unassignedID, InfluenceType.Resources);
            float influenceSum = playerBuildingsInfluence + resourcesInfluence;
            if (maxInfluence < influenceSum) {
                maxInfluence = influenceSum;
                utilityData.buildingToGuard = tile.building;
            }
        }
        return maxInfluence; // TODO: check value and make sure it is fitted and clamped to the 0->1 range.
    }
    
    public float FormTroopsNecessity()
    {
        return 0;
    }
    
    public float AttackNecessity()
    {
        return 0;
    }
    
    #endregion
    
    #region PerformMethods
    
    public void PlaceBuilding()
    {
        
    }
    
    public void RepairBuilding()
    {
        if (utilityData.buildingToRepair is null) return;
        utilityData.buildingToRepair.Repair();
    }
    
    public void GuardBuilding()
    {
        if (utilityData.buildingToGuard is null) return;
        // TODO
    }
    
    public void FormTroops()
    {
        
    }
    
    public void Attack()
    {
        
    }
    
    #endregion

    #region UtilityMethods
    
    void SelectAllTroops()
    {
        for (int i = 0; i < troops.Count; i++)
        {
            crowd.AddTroop(troops[i]);
        }

        crowd.ComputeSlowestTroopSpeed();
        crowd.LimitCrowdSpeedToSlowest();
    }

    void MoveCrowd()
    {
        crowd.ForceState("Navigate");
        Vector3 randomDirection = Random.insideUnitSphere * 10;
        NavMesh.SamplePosition(randomDirection, out var hit, 10.0f, 1);
        crowd.SetCrowdDestination(hit.position);
    }
    
    #endregion
}
