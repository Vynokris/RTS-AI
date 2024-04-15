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
        public float buildCastleNecessity    = 0;
        public float buildBarracksNecessity  = 0;
        public float buildResourceNecessity  = 0;
        public float repairBuildingNecessity = 0;
        public float guardBuildingNecessity  = 0;
        public float formTroopsNecessity     = 0;
        public float claimResourceNecessity  = 0;
        public float attackTroopNecessity    = 0;
        public float attackTileNecessity     = 0;
        public float attackCastleNecessity   = 0;
        
        public Building buildingToRepair = null;
        public Building buildingToGuard  = null;
        public Tile  naturalResourceToClaim = null;
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
        // TODO
        return Mathf.Max(utilityData.buildCastleNecessity + utilityData.buildBarracksNecessity + utilityData.buildResourceNecessity);
    }
    
    public float RepairBuildingNecessity()
    {
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
        if (utilityData.guardBuildingNecessity > 1e-3)
            return utilityData.guardBuildingNecessity;
        
        foreach (Tile tile in ownedTiles)
        {
            if (tile.buildingType is BuildingType.None) continue; // TODO: continue if the building is already guarded.
            Vector3 tilePos = tile.transform.position;
            float playerBuildingsInfluence = influenceManager.GetInfluence(tilePos, playerFactionID, InfluenceType.Buildings);
            float resourcesInfluence       = influenceManager.GetInfluence(tilePos, Faction.unassignedID, InfluenceType.Resources);
            float influenceSum = playerBuildingsInfluence + resourcesInfluence;
            if (utilityData.guardBuildingNecessity < influenceSum) {
                utilityData.guardBuildingNecessity = influenceSum;
                utilityData.buildingToGuard = tile.building;
            }
        }
        return utilityData.guardBuildingNecessity; // TODO: check value and make sure it is fitted and clamped to the 0->1 range.
    }
    
    public float FormTroopsNecessity()
    {
        if (utilityData.formTroopsNecessity > 1e-3)
            return utilityData.formTroopsNecessity;
            
        float cropsWeight = Mathf.Clamp01(crops * 0.05f);
        float tilesOwnedWeight = Mathf.Clamp01(ownedTiles.Count * 0.1f);
        float attackNecessityWeight = AttackNecessity();
        utilityData.formTroopsNecessity = (cropsWeight + tilesOwnedWeight + attackNecessityWeight) / 3;
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
            float troopsInfluenceInv = troopsInfluence >= 1 ? 1 / troopsInfluence : 1 - troopsInfluence;
            
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

        /*
        NavMeshPath pathToCastle = new();
        NavMesh.CalculatePath(spawnTile.transform.position, FactionManager.playerFaction.spawnTile.transform.position, 1, pathToCastle);
        
        Func<Vector3, bool> evaluateInfluenceOnPath = (Vector3 pos) =>
        {
            float troopsInfluence    = influenceManager.GetInfluence(pos, playerFactionID, InfluenceType.Troops);
            float buildingsInfluence = influenceManager.GetInfluence(pos, playerFactionID, InfluenceType.Buildings);
            float influence = troopsInfluence + buildingsInfluence;
            if (influence > 1) {
                enemyOnCastlePathWeight = Mathf.Clamp01(troops.Count * 0.05f);
                return true;
            }
            return false;
        };
        
        for (uint i = 0; i < pathToCastle.corners.Length; i++)
        {
            Vector3 corner = pathToCastle.corners[i];
            
            // Evaluate the corner position first.
            if (evaluateInfluenceOnPath(corner)) break;
            
            // Evaluate positions between the current corner and the next.
            if (i >= pathToCastle.corners.Length - 1) break;
            for (int j = 0; j < 50; j++)
            {
                Vector3 nextCorner = pathToCastle.corners[i + 1];
                Vector3 curToNextDir = (nextCorner - corner).normalized;
                Vector3 incrementPoint = corner + curToNextDir * 5;
                Vector3 incrementToNextDir = (nextCorner - incrementPoint).normalized;
                
                if (evaluateInfluenceOnPath(incrementPoint)) break;
                
                // Stop loop if the next corner has been passed.
                if (Vector3.Dot(curToNextDir, incrementToNextDir) < 0) break;
            }
        }
        */
        
        return Mathf.Max(utilityData.attackCastleNecessity, utilityData.attackTileNecessity, utilityData.attackTroopNecessity);
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
