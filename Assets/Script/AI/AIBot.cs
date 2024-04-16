using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.AI.Navigation;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class AIBot : Faction
{
    [Serializable] public class UtilityData
    {
        public float buildCastleNecessity     = 0;
        public float buildBarracksNecessity   = 0;
        public float buildFarmNecessity       = 0;
        public float buildLumbermillNecessity = 0;
        public float buildMineNecessity       = 0;
        public float repairBuildingNecessity  = 0;
        public float guardBuildingNecessity   = 0;
        public float formTroopsNecessity      = 0;
        public float attackTroopNecessity     = 0;
        public float attackTileNecessity      = 0;
        public float attackCastleNecessity    = 0;
        
        public Building buildingToRepair = null;
        public Building buildingToGuard  = null;
        public Troop enemyTroopToAttack = null;
        public Tile  enemyTileToAttack  = null;
    }
    
    private UtilitySystem utilitySystem;
    [SerializeField] private UtilityData utilityData;
    private MapGenerator  mapGenerator;
    private CostStorage   costStorage;
    private uint playerFactionID = Faction.unassignedID;

    public override void Awake()
    {
        base.Awake();
        
        utilitySystem = GetComponent<UtilitySystem>();
        utilitySystem.functionCallerType   = typeof(AIBot);
        utilitySystem.functionCallerScript = this;
        utilityData = new UtilityData();
        mapGenerator = FindObjectOfType<MapGenerator>();
        costStorage  = FindObjectOfType<CostStorage>();
        
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // Make sure the player faction ID is assigned.
        while (playerFactionID is Faction.unassignedID)
        {
            if (FactionManager.playerFaction is not null)
                playerFactionID = FactionManager.playerFaction.GetID();
            else
                yield return null;
        }
        
        // Evaluate and perform the utility system every 1 seconds.
        while (true)
        {
            // utilitySystem.PerformBestAction();
            utilitySystem.ChooseAction();
            yield return new WaitForSeconds(1);
            utilityData = new UtilityData();
        }
    }

    private void OnDestroy()
    {
        StopCoroutine(nameof(Run));
    }
    private void OnApplicationQuit()
    {
        StopCoroutine(nameof(Run));
    }
    
    #region EvaluationMethods
    
    public float PlaceBuildingNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.buildCastleNecessity > 1e-3 && utilityData.buildBarracksNecessity > 1e-3 && utilityData.buildFarmNecessity > 1e-3 && utilityData.buildLumbermillNecessity > 1e-3 && utilityData.buildMineNecessity > 1e-3)
            return Mathf.Max(utilityData.buildCastleNecessity, utilityData.buildBarracksNecessity, utilityData.buildFarmNecessity, utilityData.buildLumbermillNecessity, utilityData.buildMineNecessity);
        
        // Build barracks and farms when troops need to be formed.
        // TODO
        
        // Build lumbermills and mines when resources are lacking.
        // TODO
        
        // Build a castle only if the faction has more than enough lumber/stone, and there is nothing else to build.
        ActionCost castleCost = costStorage.GetBuildingCost(BuildingType.Castle);
        utilityData.buildCastleNecessity = Mathf.Clamp01(lumber / (castleCost.lumber * 2) + stone / (castleCost.stone * 2));
        utilityData.buildCastleNecessity = Mathf.Clamp01(utilityData.buildCastleNecessity - (utilityData.buildBarracksNecessity + utilityData.buildFarmNecessity + utilityData.buildLumbermillNecessity + utilityData.buildMineNecessity) * 0.2f);
        
        return Mathf.Max(utilityData.buildCastleNecessity, utilityData.buildBarracksNecessity, utilityData.buildFarmNecessity, utilityData.buildLumbermillNecessity, utilityData.buildMineNecessity);
    }
    
    public float RepairBuildingNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.repairBuildingNecessity > 1e-3)
            return utilityData.repairBuildingNecessity;
        
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None) continue;
            float healthRatioInv = 1 - tile.building.health / tile.building.maxHealth;
            
            if (utilityData.repairBuildingNecessity < healthRatioInv) {
                utilityData.repairBuildingNecessity = healthRatioInv;
                utilityData.buildingToRepair = tile.building;
            }
        }
        return utilityData.repairBuildingNecessity;
    }
    
    public float GuardBuildingNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.guardBuildingNecessity > 1e-3)
            return utilityData.guardBuildingNecessity;
        
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None) continue; // TODO: continue if the building is already guarded.
            Vector3 tilePos = tile.transform.position;
            float playerBuildingsInfluence = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Buildings);
            float resourcesInfluence       = influenceManager.GetInfluence(tilePos, Faction.unassignedID, InfluenceType.Resources);
            float influenceSum = (playerBuildingsInfluence + resourcesInfluence) * 0.5f;
            if (utilityData.guardBuildingNecessity < influenceSum) {
                utilityData.guardBuildingNecessity = influenceSum;
                utilityData.buildingToGuard = tile.building;
            }
        }
        utilityData.guardBuildingNecessity = Mathf.Clamp01(utilityData.guardBuildingNecessity);
        return utilityData.guardBuildingNecessity;
    }
    
    public float FormTroopsNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.formTroopsNecessity > 1e-3)
            return utilityData.formTroopsNecessity;
        
        // Form troops if many crops are available or many tiles are owned by the faction.
        float cropsWeight      = Mathf.Clamp01(crops * 0.01f);
        float tilesOwnedWeight = Mathf.Clamp01(ownedTiles.Count * 0.05f);
        
        // Form troops if attacking or guarding is necessary but there are not enough troops.
        int   idleTroopsCount = CountIdleTroops();
        float attackNecessityWeight = AttackNecessity()        - (idleTroopsCount + 4) * 0.1f;
        float guardNecessityWeight  = GuardBuildingNecessity() - (idleTroopsCount + 4) * 0.1f;
        
        // Form troops if the player has more than the AI.
        float playerDifferenceWeight = Mathf.Clamp01(Mathf.Max(FactionManager.playerFaction.troops.Count - troops.Count, 0) * 0.5f);
        
        utilityData.formTroopsNecessity = Mathf.Max(cropsWeight, tilesOwnedWeight, attackNecessityWeight, guardNecessityWeight, playerDifferenceWeight);
        return utilityData.formTroopsNecessity;
    }
    
    public float AttackNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.attackCastleNecessity > 1e-3 && utilityData.attackTileNecessity > 1e-3 && utilityData.attackTroopNecessity > 1e-3)
            return Mathf.Max(utilityData.attackCastleNecessity, utilityData.attackTileNecessity, utilityData.attackTroopNecessity);
        
        // Constant drive to attack the enemy castle based on number of troops formed (maxed out at 50 troops).
        utilityData.attackCastleNecessity = Mathf.Clamp01(troops.Count * 0.02f);
        
        // Target enemy tiles.
        foreach (Tile tile in FactionManager.playerFaction.ownedTiles)
        {
            Vector3 tilePos = tile.transform.position;
            float resourcesInfluence = influenceManager.GetInfluence(tilePos, Faction.unassignedID, InfluenceType.Resources);
            float troopsInfluence    = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Troops);
            float troopsInfluenceInv = troopsInfluence > 1 ? 1 / troopsInfluence : 1 - troopsInfluence;
            
            // Target strategic enemy position (resource-rich area).
            if (resourcesInfluence > utilityData.attackTileNecessity) {
                utilityData.attackTileNecessity = resourcesInfluence;
                utilityData.enemyTileToAttack   = tile;
            }

            // Target poorly defended tiles.
            if (troopsInfluenceInv > utilityData.attackTileNecessity) {
                utilityData.attackTileNecessity = troopsInfluenceInv;
                utilityData.enemyTileToAttack   = tile;
            }
        }
        
        // Target enemy troops.
        // float targetTileTroopsInfluence = influenceManager.GetInfluence(utilityData.enemyTileToAttack.transform.position, playerFactionID, InfluenceType.Troops);
        // TODO
        
        return Mathf.Max(utilityData.attackCastleNecessity, utilityData.attackTileNecessity, utilityData.attackTroopNecessity);
    }
    
    #endregion
    
    #region PerformMethods
    
    public void PlaceBuilding()
    {
        float max = PlaceBuildingNecessity();
        
        // Place a castle.
        if (utilityData.buildCastleNecessity - max < 1e-3)
        {
            // TODO
        }
        
        // Place barracks.
        else if (utilityData.buildBarracksNecessity - max < 1e-3)
        {
            // TODO
        }
        
        // Place a farm.
        else if (utilityData.buildFarmNecessity - max < 1e-3)
        {
            // TODO
        }
        
        // Place a lumbermill.
        else if (utilityData.buildLumbermillNecessity - max < 1e-3)
        {
            // TODO
        }
        
        // Place a mine.
        else if (utilityData.buildMineNecessity - max < 1e-3)
        {
            // TODO
        }
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
        // TODO
    }
    
    public void Attack()
    {
        float max = AttackNecessity();
        
        // Attack the enemy castle.
        if (utilityData.attackCastleNecessity - max < 1e-3)
        {
            SelectAllTroops();
            crowd.ForceState("Navigate");
            NavMesh.SamplePosition(FactionManager.playerFaction.spawnTile.transform.position, out var hit, 10.0f, 1);
            crowd.SetCrowdDestination(hit.position);
        }
        
        // Attack an enemy tile.
        else if (utilityData.attackTileNecessity - max < 1e-3)
        {
            crowd.RemoveAllTroops();
            SelectIdleTroops();
            crowd.ForceState("Attack");
            crowd.SetCrowdTarget(utilityData.enemyTileToAttack.building);
        }
        
        // Attack an enemy troop.
        else if (utilityData.attackTroopNecessity - max < 1e-3)
        {
            crowd.RemoveAllTroops();
            SelectIdleTroops();
            crowd.ForceState("Attack");
            crowd.SetCrowdTarget(utilityData.enemyTroopToAttack);
        }
    }
    
    #endregion

    #region UtilityMethods
    
    void SelectAllTroops()
    {
        foreach (var troop in troops)
        {
            crowd.AddTroop(troop);
        }

        crowd.ComputeSlowestTroopSpeed();
        crowd.LimitCrowdSpeedToSlowest();
    }
    
    void SelectIdleTroops()
    {
        foreach (Troop troop in troops)
        {
            if (troop.GetStateMachine().GetCurrentStateName() is "Idle")
            {
                crowd.AddTroop(troop);
            }
        }

        crowd.ComputeSlowestTroopSpeed();
        crowd.LimitCrowdSpeedToSlowest();
    }
    
    int CountIdleTroops()
    {
        int count = 0;
        foreach (Troop troop in troops)
        {
            if (troop.GetStateMachine().GetCurrentStateName() is "Idle")
            {
                count++;
            }
        }
        return count;
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
