using System;
using System.Collections.Generic;
using UnityEngine;

public class MeshStorage : MonoBehaviour
{
    [Header("Tiles")]
    public Mesh tileWater;
    public Mesh tileSand;
    public Mesh tileGrass;
    public Mesh tileStone;
    
    [Header("Props")]
    public List<Mesh> propsWater;
    public List<Mesh> propsSand;
    public List<Mesh> propsGrass;
    public List<Mesh> propsStone;
    
    [Header("Resources")]
    public Mesh resourceLumber;
    public Mesh resourceStone;
    
    [Header("Buildings")]
    public Mesh buildingCastle;
    public Mesh buildingFarm;
    public Mesh buildingLumbermill;
    public Mesh buildingMine;
    public Mesh buildingBarracks;

    public Mesh GetTile(TileType type)
    {
        return type switch
        {
            TileType.Water => tileWater,
            TileType.Sand  => tileSand,
            TileType.Grass => tileGrass,
            TileType.Stone => tileStone,
            _ => null
        };
    }

    public List<Mesh> GetProps(TileType type)
    {
        return type switch
        {
            TileType.Water => propsWater,
            TileType.Sand  => propsSand,
            TileType.Grass => propsGrass,
            TileType.Stone => propsStone,
            _ => null
        };
    }

    public Mesh GetResource(ResourceType type)
    {
        return type switch
        {
            ResourceType.Lumber => resourceLumber,
            ResourceType.Stone  => resourceStone,
            _ => null
        };
    }

    public Mesh GetBuilding(BuildingType type)
    {
        return type switch
        {
            BuildingType.Castle     => buildingCastle,
            BuildingType.Farm       => buildingFarm,
            BuildingType.Lumbermill => buildingLumbermill,
            BuildingType.Mine       => buildingMine,
            BuildingType.Barracks   => buildingBarracks,
            _ => null
        };
    }
}
