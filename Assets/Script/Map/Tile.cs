using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

public enum ResourceType
{
    None,
    Lumber,
    Stone,
    Crops,
}

public class Tile : MonoBehaviour
{
    public TileType     type         { get; private set; } = TileType.Grass;
    public PropType     propType     { get; private set; } = PropType.None;
    public ResourceType resourceType { get; private set; } = ResourceType.None;
    public GameObject   prop         { get; private set; }
    public GameObject   resource     { get; private set; }
    public Building     building     { get; private set; }
    public float noiseHeight = 0.0f;
    
    [SerializeField] private GameObject propPrefab;
    [SerializeField] private GameObject resourcePrefab;
    [SerializeField] private GameObject buildingPrefab;
    
    private MeshFilter meshFilter;
    
    public void FindMeshFilter()
    {
        meshFilter = GetComponent<MeshFilter>();
    }
    
    public float GetTileHeight()
    {
        return type == TileType.Water
            ? (gameObject.transform.localScale.y / 100 * 0.1f) - 0.1f
            : (gameObject.transform.localScale.y / 100 * 0.2f) - 0.2f;
    }

    public void SetType(TileType tileType, Mesh tileMesh)
    {
        type = tileType;
        meshFilter.mesh = tileMesh;
        if (type == TileType.Water)
            gameObject.layer = LayerMask.NameToLayer("MapTileBlocking");
    }

    public bool SetProp(Mesh propMesh)
    {
        if (resource is not null || building is not null) return false;
        if (prop is not null) { Destroy(prop); }
        
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

    public bool SetResource(ResourceType _resourceType, Mesh resourceMesh)
    {
        if (prop is not null || building is not null) return false;
        if (resource is not null) { Destroy(resource.gameObject); }
        
        resource = Instantiate(resourcePrefab, transform.parent);
        resource.GetComponent<MeshFilter>().mesh = resourceMesh;
        resource.transform.position  += new Vector3(0, GetTileHeight(), 0);
        resource.transform.localScale = new Vector3(100, 100, 100);
        resourceType = _resourceType;
        return true;
    }

    public bool SetBuilding(BuildingType buildingType, Mesh buildingMesh)
    {
        if (propType is PropType.Unbreakable) { return false; }
        if (propType is PropType.Breakable && prop is not null) {
            Destroy(prop);
            propType = PropType.None;
        }
        if (building is not null) { Destroy(building.gameObject); }
        
        building = Instantiate(buildingPrefab, transform.parent).GetComponent<Building>();
        building.GetComponent<MeshFilter>().mesh = buildingMesh;
        building.SetType(buildingType);
        return true;
    }

}
