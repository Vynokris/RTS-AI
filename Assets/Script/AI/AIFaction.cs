using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class AIFaction : Faction
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
        public float attackBuildingNecessity  = 0;
        
        public Building buildingToRepair = null;
        public Building buildingToGuard  = null;
        public Building buildingToAttack = null;
    }
    
    private UtilitySystem utilitySystem;
    [SerializeField] private UtilityData utilityData;
    private MapGenerator  mapGenerator;
    private uint playerFactionID = Faction.unassignedID;

    public override void Awake()
    {
        base.Awake();
        
        utilitySystem = GetComponent<UtilitySystem>();
        utilitySystem.functionCallerType   = typeof(AIFaction);
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
        
        // Maintain a certain proportion of each building.
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
    
    public float FormTroopsNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.formTroopsNecessity > 1e-3)
            return utilityData.formTroopsNecessity;
        
        // Return 0 if no barracks have been built.
        if (ownedBuildings[BuildingType.Barracks].Count <= 0)
            return utilityData.formTroopsNecessity = 0;
        
        // Form troops if many crops are available or many tiles are owned by the faction.
        float cropsWeight      = Mathf.Clamp01(crops * 0.01f);
        float tilesOwnedWeight = Mathf.Clamp01(ownedTiles.Count / (troops.Count * 2f));
        
        // Form troops if there are not many idle troops.
        // int   idleTroops = CountIdleTroops();
        // float attackNecessityWeight = (float)troops.Count / idleTroops;
        float attackNecessityWeight = 0; // TODO
        
        // Form troops if the player has more than the AI.
        float playerDifferenceWeight = Mathf.Clamp(Mathf.Max(FactionManager.playerFaction.troops.Count - troops.Count, 0) * 0.5f, 0, 0.9f);
        
        utilityData.formTroopsNecessity = Mathf.Max(cropsWeight, tilesOwnedWeight, attackNecessityWeight, playerDifferenceWeight);
        return utilityData.formTroopsNecessity;
    }

    public float GuardBuildingNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.guardBuildingNecessity > 1e-3) return utilityData.guardBuildingNecessity;
        
        // Guard buildings that are in danger.
        foreach (Tile tile in ownedTiles)
        {
            Vector3 tilePos = tile.transform.position;
            float enemyTroopsInfluence = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Troops);

            if (enemyTroopsInfluence > utilityData.guardBuildingNecessity) {
                utilityData.guardBuildingNecessity = enemyTroopsInfluence;
                utilityData.buildingToGuard        = tile.building;
            }
        }
        return utilityData.guardBuildingNecessity;
    }
    
    public float AttackBuildingNecessity()
    {
        // Early exit if the necessity was already evaluated this frame.
        if (utilityData.attackBuildingNecessity > 1e-3) return utilityData.attackBuildingNecessity;
        
        // Return 0 if no barracks have been built.
        if (ownedBuildings[BuildingType.Barracks].Count <= 0)
            return utilityData.formTroopsNecessity = 0;
        
        // Target strategic enemy positions (resource-rich areas), that are poorly defended.
        foreach (Tile tile in FactionManager.playerFaction.ownedTiles)
        {
            Vector3 tilePos = tile.transform.position;
            float resourcesInfluence = influenceManager.GetInfluence(tilePos, Faction.unassignedID, InfluenceType.Resources);
            float troopsInfluence    = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Troops);
            float troopsInfluenceInv = troopsInfluence > 1 ? 1 / troopsInfluence : 1 - troopsInfluence;
            bool  isSpawn            = tile == FactionManager.playerFaction.spawnTile;
            float overallInfluence   = Mathf.Clamp01(resourcesInfluence * 0.8f + troopsInfluenceInv * 0.2f + (isSpawn ? troops.Count * 0.02f : 0));
            
            if (overallInfluence > utilityData.attackBuildingNecessity) {
                utilityData.attackBuildingNecessity = overallInfluence;
                utilityData.buildingToAttack        = tile.building;
            }
        }
        return utilityData.attackBuildingNecessity * Mathf.Clamp01(troops.Count / ((FactionManager.playerFaction.troops.Count + 1) * 0.75f));
    }
    
    #endregion
    
    #region PerformMethods
    
    public void PlaceBuilding()
    {
        float max = PlaceBuildingNecessity();
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
                // Find a random tile not too far from the spawn point where the building can be placed.
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
                if (!closestTile) break;
                CreateBuilding(closestTile, buildingType);
                break;
            }
        }
    }
    
    public void FormTroops()
    {
        bool canTrain = ownedBuildings[BuildingType.Barracks].Count > 0;
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

    public void GuardBuilding()
    {
        crowd.RemoveAllTroops();
        SelectTroops("Idle");
        SelectTroops("Guard", null, -1, false);
        crowd.ForceState("Guard");
        crowd.SetCrowdTarget(utilityData.buildingToGuard);
    }
    
    public void AttackBuilding()
    {
        crowd.RemoveAllTroops();
        SelectTroops("Idle");
        SelectTroops("Guard", null, -1, false);
        crowd.ForceState("Attack");
        crowd.SetCrowdTarget(utilityData.buildingToAttack);
    }
    
    #endregion

    #region UtilityMethods
    
    void SelectTroops(string state = null, TroopType? troopType = null, int maxSelected = -1, bool? withNearbyEnemies = null)
    {
        int counter = 0;
        foreach (Troop troop in troops)
        {
            bool enemiesNearby = troop.GetBlackBoard().GetNearingEnemies().Count > 0;
            
            if ((troopType         is null || troop.type == troopType) &&
                (state             is null || troop.GetStateMachine().GetCurrentStateName() == state) &&
                (withNearbyEnemies is null || enemiesNearby == withNearbyEnemies))
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
