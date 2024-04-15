using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class AIBot : Faction
{
    private UtilitySystem utilitySystem;

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
            if (lowestRatio > healthRatio)
                lowestRatio = healthRatio;
        }
        return lowestRatio;
    }
    
    public float FormTroopsNecessity()
    {
        return 0;
    }
    
    public float GuardBuildingNecessity()
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
        float    lowestRatio      = 1;
        Building buildingToRepair = null;
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None || tile.building.repairing) continue;
            float healthRatio = tile.building.health / tile.building.maxHealth;
            if (lowestRatio > healthRatio) {
                lowestRatio = healthRatio;
                buildingToRepair = tile.building;
            }
        }
        
        if (buildingToRepair is not null)
        {
            buildingToRepair.Repair();
        }
    }
    
    public void FormTroops()
    {
        
    }
    
    public void GuardBuilding()
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
