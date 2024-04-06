using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Water,
    Sand,
    Grass,
    Stone,
}

public enum PropType
{
    None,
    Breakable,
    Unbreakable,
}

// NOTE: ResourceType and BuildingType have values that need to correspond:
// 0 = ResourceType.None   = BuildingType.None
// 1 = ResourceType.Crops  = BuildingType.Farm
// 2 = ResourceType.Lumber = BuildingType.Lumbermill
// 3 = ResourceType.Stone  = BuildingType.Mine

public enum ResourceType
{
    None,
    Crops,
    Lumber,
    Stone,
}

public enum BuildingType
{
    None,
    Farm,
    Lumbermill,
    Mine,
    Barracks,
    Castle,
}


public class Tile : MonoBehaviour
{
    public Faction      owningFaction { get; private set; } = null;
    public TileType     type          { get; private set; } = TileType.Grass;
    public PropType     propType      { get; private set; } = PropType.None;
    public ResourceType resourceType  { get; private set; } = ResourceType.None;
    public BuildingType buildingType  { get; private set; } = BuildingType.None;
    public GameObject   prop          { get; private set; } = null;
    public GameObject   resource      { get; private set; } = null;
    public Building     building      { get; private set; } = null;
    [HideInInspector] public float noiseHeight = 0.0f;
    
    [SerializeField] private GameObject propPrefab;
    [SerializeField] private GameObject resourcePrefab;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private GameObject resourceBuildingPrefab;
    [SerializeField] private GameObject barracksBuildingPrefab;
    
    private MeshFilter  meshFilter;
    private MeshStorage meshStorage;

    public void Start()
    {
        enabled = false;
    }

    public void FindMeshFilter()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void SetMeshStorage(MeshStorage _meshStorage)
    {
        meshStorage = _meshStorage;
    }
    
    public float GetTileHeight()
    {
        return type == TileType.Water
            ? gameObject.transform.localScale.y / 100 * 0.1f - 0.1f
            : gameObject.transform.localScale.y / 100 * 0.2f - 0.2f;
    }

    public void SetFaction(Faction faction)
    {
        owningFaction = faction;
    }

    public void SetType(TileType tileType)
    {
        type = tileType;
        meshFilter.mesh = meshStorage.GetTile(tileType);
        if (type == TileType.Water)
            gameObject.layer = LayerMask.NameToLayer("MapTileBlocking");
    }

    public bool TrySetProp(Mesh propMesh)
    {
        if (resource is not null || building is not null) return false;
        if (prop is not null) { Destroy(prop); prop = null; }
        
        prop = Instantiate(propPrefab, transform.parent);
        prop.GetComponent<MeshFilter>().mesh = propMesh;
        
        if (type == TileType.Grass)
        {
            prop.transform.position += new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 
                                                   GetTileHeight() + 0.2f, 
                                                   UnityEngine.Random.Range(-0.2f, 0.2f));
        }
        else
        {
            prop.transform.position  += new Vector3(0, GetTileHeight(), 0);
            prop.transform.localScale = new Vector3(100, 100, 100);
        }
        
        propType = type is TileType.Sand or TileType.Grass ? PropType.Breakable : PropType.Unbreakable;
        if (propType == PropType.Unbreakable)
        {
            gameObject.layer = LayerMask.NameToLayer("MapTileBlocking");
        }
        return true;
    }

    public bool TrySetResource(ResourceType _resourceType)
    {
        if (prop is not null || building is not null) return false;
        if (resource is not null) { Destroy(resource); resource = null; }
        
        resourceType = _resourceType;
        resource = Instantiate(resourcePrefab, transform.parent);
        resource.GetComponent<MeshFilter>().mesh = meshStorage.GetResource(resourceType);
        resource.transform.position  += new Vector3(0, GetTileHeight(), 0);
        resource.transform.localScale = new Vector3(100, 100, 100);
        return true;
    }

    public bool CanSetBuilding(BuildingType _buildingType)
    {
        // Check building: return if a building exists already, return if building can't be placed on tile.
        if (buildingType != BuildingType.None || building is not null) { return false; }
        switch (_buildingType)
        {
            case BuildingType.Farm when type is not TileType.Grass:
            case BuildingType.Lumbermill when resourceType is not ResourceType.Lumber:
            case BuildingType.Mine when resourceType is not ResourceType.Stone:
                return false;
        }

        // Check the prop: return if unbreakable.
        if (propType is PropType.Unbreakable) {
            return false;
        }
        
        // Check resource: make sure the building corresponds.
        if (resourceType is ResourceType.Lumber or ResourceType.Stone)
        {
            if ((int)resourceType != (int)_buildingType) {
                return false;
            }
        }
        return true;
    }

    /// Should only ever be used if CanSetBuilding returns true.
    public void ForceSetBuilding(BuildingType _buildingType)
    {
        // Destroy the building if it exists.
        if (building is not null)
        {
            Destroy(building);
            building = null;
            buildingType = BuildingType.None;
        }
        
        // Destroy the prop if it exists.
        if (prop is not null)
        {
            Destroy(prop);
            prop = null;
            propType = PropType.None;
        }
        
        // Destroy the resource if it exists.
        if (resource is not null)
        {
            Destroy(resource);
            resource = null;
            resourceType = ResourceType.None;
        }
        
        // Create the building.
        buildingType = _buildingType;
        GameObject instantiatedBuilding = buildingType switch
        {
            BuildingType.Farm or BuildingType.Lumbermill or BuildingType.Mine => resourceBuildingPrefab,
            BuildingType.Barracks => barracksBuildingPrefab,
            _ => buildingPrefab
        };
        building = Instantiate(instantiatedBuilding, transform.parent).GetComponent<Building>();
        building.SetOwningTile(this);
        building.GetComponent<MeshFilter>().mesh = meshStorage.GetBuilding(buildingType);
        building.transform.position  += new Vector3(0, GetTileHeight(), 0);
        building.transform.localScale = new Vector3(100, 100, 100);
        if (buildingType is BuildingType.Farm or BuildingType.Lumbermill or BuildingType.Mine)
            enabled = true;
    }

    public bool TrySetBuilding(BuildingType _buildingType)
    {
        if (!CanSetBuilding(_buildingType)) {
            return false;
        }
        ForceSetBuilding(_buildingType);
        return true;
    }

    public void RemoveBuilding()
    {
        if (building is not null) {
            Destroy(building);
            building = null;
        }

        BuildingType prevBuildingType = buildingType;
        buildingType = BuildingType.None;
        switch (prevBuildingType)
        {
            case BuildingType.Lumbermill:
                TrySetResource(ResourceType.Lumber);
                break;
            case BuildingType.Mine:
                TrySetResource(ResourceType.Stone);
                break;
        }
        
        owningFaction.RemoveOwnership(this);
        enabled = false;
    }
}
