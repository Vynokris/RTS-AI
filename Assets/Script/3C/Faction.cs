using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class Faction : MonoBehaviour
{
    protected static uint maxID = 0;
    public const uint unassignedID = uint.MaxValue;
    
    public uint id { get; private set; }
    public string designation = "";
    public Color color;
    public float crops  = 10;
    public float lumber = 10;
    public float stone  = 10;
    public Tile spawnTile { get; protected set; } = null;
    protected List<Tile> ownedTiles = new();

    protected Crowd crowd = new();
    protected List<Troop> troops = new();
    
    protected TroopStorage troopStorage;

    public Faction() { id = maxID++; }
    public uint GetID() { return id; }

    public virtual void Start()
    {
        troopStorage = FindObjectOfType<TroopStorage>();
        
        NavMesh.SamplePosition(spawnTile.transform.position, out NavMeshHit navMeshHit, float.MaxValue, NavMesh.AllAreas);
        for (int i = 0; i < 5; i++)
        {
            SpawnTroop(TroopType.Knight, navMeshHit.position + Vector3.up);
            SpawnTroop(TroopType.Golem,  navMeshHit.position + Vector3.up);
        }
    }

    public void TakeOwnership(Tile tile, bool setAsSpawn = false)
    {
        tile.SetFaction(this);
        ownedTiles.Add(tile);
        if (setAsSpawn) spawnTile = tile;
    }

    public void RemoveOwnership(Tile tile)
    {
        ownedTiles.Remove(tile);
        tile.SetFaction(null);
    }

    public void SpawnTroop(TroopType type, Vector3 position)
    {
        Troop troop = Instantiate(troopStorage.GetTroopPrefab(type), position, Quaternion.identity).GetComponent<Troop>();
        troop.SetFaction(this);
        troops.Add(troop);
    }

    public void DestroyTroop(Troop troop)
    {
        troops.Remove(troop);
        Destroy(troop);
    }
}
