using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Tile : MonoBehaviour
{
    public TileType     type               { get; private set; } = TileType.Grass;
    public PropType     propType           { get; private set; } = PropType.None;
    public ResourceType resourceType       { get; private set; } = ResourceType.None;
    public bool         harvestingResource { get; private set; } = false;
    public GameObject   propObject     { get; private set; }
    public GameObject   resourceObject { get; private set; }
    public float noiseHeight = 0.0f;
    
    private MeshFilter meshFilter;
    
    public void FindMeshFilter()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void SetType(TileType _tileType, Mesh tileMesh)
    {
        type = _tileType;
        meshFilter.mesh = tileMesh;
        if (type == TileType.Water)
            gameObject.layer = LayerMask.NameToLayer("MapTileBlocking");
    }

    public void SetProp(GameObject _propObject, Mesh propMesh)
    {
        propType = type is TileType.Sand or TileType.Grass ? PropType.Breakable : PropType.Unbreakable;
        propObject = _propObject;
        propObject.GetComponent<MeshFilter>().mesh = propMesh;
        if (propType == PropType.Unbreakable)
            gameObject.layer = LayerMask.NameToLayer("MapTileBlocking");
    }

    public void SetResource(ResourceType _resourceType, GameObject _resourceObject, Mesh resourceMesh)
    {
        resourceType = _resourceType;
        resourceObject = _resourceObject;
        resourceObject.GetComponent<MeshFilter>().mesh = resourceMesh;
    }

    public void SetHarvestingResource(bool _harvestingResource, Mesh resourceMesh)
    {
        harvestingResource = _harvestingResource;
        resourceObject.GetComponent<MeshFilter>().mesh = resourceMesh;
    }

    public float GetTileHeight()
    {
        return type == TileType.Water
            ? (gameObject.transform.localScale.y / 100 * 0.1f) - 0.1f
            : (gameObject.transform.localScale.y / 100 * 0.2f) - 0.2f;
    }
}

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

