using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Faction : MonoBehaviour
{
    protected Crowd crowd = new();
    public const uint unassignedID = uint.MaxValue;
    
    public uint id { get; private set; }
    public string designation = "";
    public Color color;
    public float crops  = 10;
    public float lumber = 10;
    public float stone  = 10;
    public Tile spawnTile { get; protected set; } = null;
    public List<Tile> ownedTiles { get; protected set; } = new();
    public Dictionary<BuildingType, List<Building>> ownedBuildings { get; protected set; } = new();
    public List<Troop> troops { get; protected set; } = new();
    protected UnityEvent spawnTileAssigned = new();
    
    protected TroopStorage troopStorage;
    protected CostStorage  costStorage;
    protected InfluenceManager influenceManager;

    public Faction() { id = unassignedID; }
    public uint GetID() { return id; }

    public virtual void Awake()
    {
        troopStorage     = FindObjectOfType<TroopStorage>();
        costStorage      = FindObjectOfType<CostStorage>();
        influenceManager = FindObjectOfType<InfluenceManager>();
        
        foreach (BuildingType type in Enum.GetValues(typeof(BuildingType))) {
            ownedBuildings[type] = new List<Building>();
        }
        
        spawnTileAssigned.AddListener(() =>
        {
            NavMesh.SamplePosition(spawnTile.transform.position + Vector3.up * (spawnTile.GetTileHeight() + .5f), out NavMeshHit navMeshHit, float.MaxValue, NavMesh.AllAreas);
            if (!navMeshHit.hit) return;
            for (int i = 0; i < 3; i++)
            {
                SpawnTroop(TroopType.Knight, navMeshHit.position);
                SpawnTroop(TroopType.Archer, navMeshHit.position);
            }
            crowd.SetCoordinator(Instantiate(troopStorage.GetTroopPrefab(TroopType.Coordinator), navMeshHit.position, Quaternion.identity));
        });
    }
    
    public void AssignID(uint _id) { if (id is unassignedID) id = _id; }

    public void TakeOwnership(Tile tile, bool setAsSpawn = false)
    {
        tile.SetFaction(this);
        ownedTiles.Add(tile);
        if (setAsSpawn) {
            spawnTile = tile;
            spawnTileAssigned.Invoke();
        }
        
        if (!influenceManager)
            influenceManager = FindObjectOfType<InfluenceManager>();
        
        if (tile.buildingType is not BuildingType.None)
        {
            if (tile.buildingType is BuildingType.Lumbermill or BuildingType.Mine)
            {
                influenceManager.ClaimResource(id, tile.transform.position);
            }
            influenceManager.AddBuilding(id, tile.transform.position);
        }
    }

    public void RemoveOwnership(Tile tile)
    {
        if (tile.buildingType is not BuildingType.None)
        {
            if (tile.buildingType is BuildingType.Lumbermill or BuildingType.Mine)
            {
                influenceManager.UnclaimResource(tile.transform.position);
            }
            influenceManager.RemoveBuilding(tile.transform.position);
        }
        
        ownedTiles.Remove(tile);
        tile.SetFaction(null);
    }

    public Troop SpawnTroop(TroopType type, Vector3 position)
    {
        Troop troop = Instantiate(troopStorage.GetTroopPrefab(type), position, Quaternion.identity).GetComponent<Troop>();
        troop.SetFaction(this);
        troops.Add(troop);

        return troop;
    }

    public void DestroyTroop(Troop troop)
    {
        troops.Remove(troop);
        crowd.RemoveTroop(troop);
        Destroy(troop.gameObject);
    }

    public bool CreateBuilding(Tile tile, BuildingType buildingType)
    {
        ActionCost buildCost = costStorage.GetBuildingCost(buildingType);
        bool canSetBuilding  = tile.CanSetBuilding(buildingType);
        bool canPerform      = buildCost.CanPerform(crops, lumber, stone);

        if (!canSetBuilding || !canPerform) return false;
        tile.ForceSetBuilding(buildingType);
        buildCost.ForcePerform(ref crops, ref lumber, ref stone);
        TakeOwnership(tile);
        ownedBuildings[buildingType].Add(tile.building);
        return true;
    }

    public bool DestroyBuilding(Tile tile, bool regainBuildResources)
    {
        if (tile.buildingType is BuildingType.None) 
            return false;
        
        ActionCost buildCost = costStorage.GetBuildingCost(tile.buildingType);
        ownedBuildings[tile.buildingType].Remove(tile.building);
        tile.RemoveBuilding();
        
        if (regainBuildResources) 
            buildCost.Undo(ref crops, ref lumber, ref stone);

        if (ownedBuildings[BuildingType.Castle].Count == 0)
        {
            FindObjectOfType<FactionManager>().RequestGameLost(this);
        }

        return true;
    }

    public Crowd GetCrowd()
    {
        return crowd;
    }
}
