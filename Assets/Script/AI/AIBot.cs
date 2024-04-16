using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.AI.Navigation;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Analytics;
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
    private uint playerFactionID = Faction.unassignedID;

    public override void Awake()
    {
        base.Awake();
        
        utilitySystem = GetComponent<UtilitySystem>();
        utilitySystem.functionCallerType   = typeof(AIBot);
        utilitySystem.functionCallerScript = this;
        utilityData = new UtilityData();
        mapGenerator = FindObjectOfType<MapGenerator>();
        
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
            utilitySystem.PerformBestAction();
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

        // Always start by building a lumbermill, a mine, and a farm.
        if (ownedBuildings[BuildingType.Lumbermill].Count <= 0) {
            return utilityData.buildLumbermillNecessity = 1;
        }
        if (ownedBuildings[BuildingType.Mine].Count <= 0) {
            return utilityData.buildMineNecessity = 1;
        }
        if (ownedBuildings[BuildingType.Farm].Count <= 0) {
            return utilityData.buildFarmNecessity = 1;
        }
        
        // Build barracks when no idle troops are available, and there are enough resources available.
        /*
        int        idleTroopsCount  = CountIdleTroops();
        float      idleTroopsWeight = idleTroopsCount <= 0 ? 1 : 1f / idleTroopsCount;
        ActionCost barracksCost     = costStorage.GetBuildingCost(BuildingType.Barracks);
        Vector3    barracksRatios   = barracksCost.RatioOwnedToNecessary(crops, lumber, stone);
        utilityData.buildBarracksNecessity = idleTroopsWeight * Mathf.Min(barracksRatios.x, barracksRatios.y, barracksRatios.z);
        
        // Build more farms when the number of barracks increases.
        utilityData.buildFarmNecessity = Mathf.Clamp01((ownedBuildings[BuildingType.Barracks].Count - (float)ownedBuildings[BuildingType.Farm].Count) * 0.5f);
        */
        
        // Build lumbermills and mines when resources are lacking.
        utilityData.buildLumbermillNecessity = Mathf.Clamp01(0.1f  * ownedTiles.Count / ownedBuildings[BuildingType.Lumbermill].Count);
        utilityData.buildMineNecessity       = Mathf.Clamp01(0.1f  * ownedTiles.Count / ownedBuildings[BuildingType.Mine].Count);
        utilityData.buildFarmNecessity       = Mathf.Clamp01(0.09f * ownedTiles.Count / ownedBuildings[BuildingType.Farm].Count);
        utilityData.buildBarracksNecessity   = Mathf.Clamp01(0.07f * ownedTiles.Count / ownedBuildings[BuildingType.Barracks].Count);
        
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
        return 0;
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.guardBuildingNecessity > 1e-3)
            return utilityData.guardBuildingNecessity;
        
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None) continue; // TODO: continue if the building is already guarded.
            Vector3 tilePos = tile.transform.position;
            float playerBuildingsInfluence = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Buildings);
            float playerTroopsInfluence    = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Troops);
            float resourcesInfluence       = influenceManager.GetInfluence(tilePos, Faction.unassignedID, InfluenceType.Resources);
            float influenceMax = Mathf.Max(playerBuildingsInfluence, playerTroopsInfluence, resourcesInfluence);
            if (utilityData.guardBuildingNecessity < influenceMax) {
                utilityData.guardBuildingNecessity = influenceMax;
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
        // TODO
        
        return Mathf.Max(utilityData.attackCastleNecessity, utilityData.attackTileNecessity, utilityData.attackTroopNecessity);
    }
    
    #endregion
    
    #region PerformMethods
    
    public void PlaceBuilding()
    {
        float max = PlaceBuildingNecessity();
        // TODO: Check that this works.
        BuildingType buildingType = Mathf.Abs(utilityData.buildCastleNecessity     - max) < 1e-3 ? BuildingType.Castle
                                  : Mathf.Abs(utilityData.buildBarracksNecessity   - max) < 1e-3 ? BuildingType.Barracks
                                  : Mathf.Abs(utilityData.buildFarmNecessity       - max) < 1e-3 ? BuildingType.Farm
                                  : Mathf.Abs(utilityData.buildLumbermillNecessity - max) < 1e-3 ? BuildingType.Lumbermill
                                  : Mathf.Abs(utilityData.buildMineNecessity       - max) < 1e-3 ? BuildingType.Mine
                                  : BuildingType.None;
        
        ActionCost buildCost = costStorage.GetBuildingCost(buildingType);
        if (!buildCost.CanPerform(crops, lumber, stone)) return;

        switch (buildingType)
        {
            case BuildingType.Castle:
            case BuildingType.Barracks:
            case BuildingType.Farm:
            {
                float randRange = 3;
                for (int i = 0; i < 100; i++)
                {
                    Vector3 randTilePos = spawnTile.transform.position + new Vector3(Random.Range(-randRange, randRange), 0, Random.Range(-randRange, randRange));
                    Ray randTileRay = new Ray(randTilePos + Vector3.up * 50, Vector3.down);
                    if (!Physics.Raycast(randTileRay, out RaycastHit hit, float.MaxValue, 1 << LayerMask.NameToLayer("MapTile"))) continue;
                    Tile tile = hit.transform.gameObject.GetComponent<Tile>();
                    if (!tile) continue;

                    if (!tile.CanSetBuilding(buildingType)) {
                        randRange++;
                        continue;
                    }
                    CreateBuilding(tile, buildingType);
                    break;
                }
                break;
            }

            case BuildingType.Lumbermill:
            case BuildingType.Mine:
            {
                // Find the closest unclaimed resource to the faction's spawn tile.
                Tile closestTile = null;
                float closestTileDist = float.MaxValue;
                foreach (Tile resourceTile in mapGenerator.naturalResources)
                {
                    if (resourceTile.owningFaction is not null || (int)resourceTile.resourceType != (int)buildingType) continue;
                    float dist = Vector3.Distance(spawnTile.transform.position, resourceTile.transform.position);
                    if (dist < closestTileDist)
                    {
                        closestTileDist = dist;
                        closestTile = resourceTile;
                    }
                }

                // Create the building if possible.
                if (!closestTile) break;
                CreateBuilding(closestTile, buildingType);
                break;
            }
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
        crowd.RemoveAllTroops();
        SelectTroops("Idle", TroopType.Knight, 5);
        crowd.ForceState("Guard");
        crowd.SetCrowdTarget(utilityData.buildingToGuard);
    }
    
    public void FormTroops()
    {
        bool canTrain = true;
        while (canTrain)
        {
            foreach (Building barracks in ownedBuildings[BuildingType.Barracks])
            {
                bool success = ((BarracksBuilding)barracks).AddTroopToTrain((TroopType)Random.Range(1, 4));
                if (!success) {
                    canTrain = false;
                    break;
                }
            }
        }
    }
    
    public void Attack()
    {
        float max = AttackNecessity();
        
        // Attack the enemy castle.
        if (Mathf.Abs(utilityData.attackCastleNecessity - max) < 1e-3)
        {
            SelectTroops();
            crowd.ForceState("Navigate");
            NavMesh.SamplePosition(FactionManager.playerFaction.spawnTile.transform.position, out var hit, 10.0f, 1);
            crowd.SetCrowdDestination(hit.position);
        }
        
        // Attack an enemy tile.
        else if (Mathf.Abs(utilityData.attackTileNecessity - max) < 1e-3)
        {
            crowd.RemoveAllTroops();
            SelectTroops("Idle");
            crowd.ForceState("Attack");
            crowd.SetCrowdTarget(utilityData.enemyTileToAttack.building);
        }
        
        // Attack an enemy troop.
        else if (Mathf.Abs(utilityData.attackTroopNecessity - max) < 1e-3)
        {
            crowd.RemoveAllTroops();
            SelectTroops("Idle");
            crowd.ForceState("Attack");
            crowd.SetCrowdTarget(utilityData.enemyTroopToAttack);
        }
    }
    
    #endregion

    #region UtilityMethods
    
    void SelectTroops(string state = null, TroopType? troopType = null, int maxSelected = -1)
    {
        int counter = 0;
        foreach (Troop troop in troops)
        {
            if ((troopType is null || troop.type == troopType) &&
                (state is null || troop.GetStateMachine().GetCurrentStateName() == state))
            {
                crowd.AddTroop(troop);
                counter++;
            }
            
            if (maxSelected > 0 && counter >= maxSelected)
                break;
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
