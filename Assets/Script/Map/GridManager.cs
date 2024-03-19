using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMesh;
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject propPrefab;
    [SerializeField] private GameObject resourcePrefab;

    [Header("Meshes")]
    [SerializeField] private Gradient thresholds;
    [SerializeField] private Mesh waterTile;
    [SerializeField] private Mesh sandTile;
    [SerializeField] private Mesh grassTile;
    [SerializeField] private Mesh stoneTile;
    [SerializeField] private List<Mesh> waterProps;
    [SerializeField] private List<Mesh> sandProps;
    [SerializeField] private List<Mesh> grassProps;
    [SerializeField] private List<Mesh> stoneProps;
    [SerializeField] private Mesh lumberResource;
    [SerializeField] private Mesh stoneResource;
    [SerializeField] private Mesh lumberHarvesting;
    [SerializeField] private Mesh stoneHarvesting;
    [SerializeField] private Mesh cropHarvesting;

    [Header("Map Generation")] 
    [SerializeField] private bool autoUpdate = false;
    [SerializeField] private bool update = false;
    [SerializeField] private Vector2 mapSize;
    [SerializeField] private Vector2 offset;
    [SerializeField] private int seed = 0;
    [SerializeField] private int octaves = 3;
    [SerializeField] private float perlinZoom = 14.0f;
    [SerializeField] private float perlinHeight = 2.0f;
    [Range(0, 100)] [SerializeField] private int resourceSpawnChance = 1;
    [Range(0, 100)] [SerializeField] private int propSpawnChance = 1;
    
    private List<Tile> tiles = new();

    private float timer = 0.1f;
    private float timeSpent = 0.0f;

    private Mesh GetTileMesh(TileType tileType)
    {
        return tileType switch
        {
            TileType.Water => waterTile,
            TileType.Sand  => sandTile,
            TileType.Grass => grassTile,
            TileType.Stone => stoneTile,
            _ => null
        };
    }

    private List<Mesh> GetPropMeshes(TileType tileType)
    {
        return tileType switch
        {
            TileType.Water => waterProps,
            TileType.Sand  => sandProps,
            TileType.Grass => grassProps,
            TileType.Stone => stoneProps,
            _ => null
        };
    }

    private Mesh GetResourceMesh(ResourceType resourceType, bool harvesting)
    {
        return !harvesting ? resourceType switch
        {
            ResourceType.Lumber => lumberResource,
            ResourceType.Stone  => stoneResource,
            ResourceType.Crops  => null,
            _ => null
        }
            : resourceType switch
        {
            ResourceType.Lumber => lumberHarvesting,
            ResourceType.Stone  => stoneHarvesting,
            ResourceType.Crops  => cropHarvesting,
            _ => null
        };
    }

    private bool SetTileProp(Tile tile)
    {
        List<Mesh> propMeshes = GetPropMeshes(tile.type);
        if (propMeshes is null || propMeshes.Count <= 0) return false;
        
        bool spawnProp = Random.Range(0, 100) < propSpawnChance;
        if (!spawnProp) return false;
        
        int random = Random.Range(0, propMeshes.Count);

        GameObject obj = Instantiate(propPrefab, tile.gameObject.transform.parent);

        if (tile.type == TileType.Grass)
        {
            float treeOffset = Random.Range(-0.2f, 0.2f);
            obj.transform.position += new Vector3(treeOffset, tile.GetTileHeight() + 0.2f, treeOffset);
        }

        else
        {
            obj.transform.position += new Vector3(0, tile.GetTileHeight(), 0);
            obj.transform.localScale = new Vector3(100, 100, 100);
        }

        tile.SetProp(obj, propMeshes[random]);
        return true;
    }

    private bool SetTileResource(Tile tile)
    {
        if (tile.type is TileType.Water or TileType.Sand) return false;
        
        bool spawnResource = Random.Range(0, 100) < resourceSpawnChance;
        if (!spawnResource) return false;

        ResourceType resourceType = tile.type switch
        {
            TileType.Grass => Random.Range(0, 3) == 0 ? ResourceType.Stone : ResourceType.Lumber,
            TileType.Stone => ResourceType.Stone,
            _ => ResourceType.None,
        };

        GameObject obj = Instantiate(propPrefab, tile.gameObject.transform.parent);
        obj.transform.position += new Vector3(0, tile.GetTileHeight(), 0);
        obj.transform.localScale = new Vector3(100, 100, 100);

        tile.SetResource(resourceType, obj, GetResourceMesh(resourceType, false));
        return true;
    }

    public void BuildMap()
    {
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int i = 0; i < mapSize.y; i++)
        {
            for (int j = 0; j < mapSize.x; j++)
            {
                var worldPos = grid.GetCellCenterWorld(new Vector3Int(j, i));
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.AngleAxis(60 * Random.Range(0, 6), Vector3.up), transform);

                float totalSample = 0f;
                float frequency = 1f;
                float amplitude = 1f;

                for (int k = 0; k < octaves; k++)
                {
                    float x = worldPos.x / perlinZoom * frequency + octaveOffsets[k].x;
                    float y = worldPos.z / perlinZoom * frequency + octaveOffsets[k].y;

                    float sample = Mathf.PerlinNoise(x, y) * 2 - 1;
                    totalSample += sample * amplitude;

                    frequency *= 2f;
                    amplitude *= 0.5f;
                }

                if (totalSample > maxNoiseHeight)
                    maxNoiseHeight = totalSample;
                else if (totalSample < minNoiseHeight)
                    minNoiseHeight = totalSample;

                Tile tile = tileObj.GetComponentInChildren<Tile>();
                tile.noiseHeight = totalSample;
                tile.FindMeshFilter();
                tiles.Add(tile);
            }
        }

        foreach (var tile in tiles)
        {
            float finalSample = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, tile.noiseHeight);
            float scaleFactor = finalSample * perlinHeight * 100;
            tile.transform.localScale += new Vector3(0, scaleFactor, 0);
            Color color = thresholds.Evaluate(finalSample);
            TileType tileType = (TileType)Math.Clamp(Array.FindIndex(thresholds.colorKeys, element => element.color == color), 0, 100);
            
            if (tileType == TileType.Water)
                LevelWater(tile);

            tile.SetType(tileType, GetTileMesh(tileType));

            if (!SetTileResource(tile))
                SetTileProp(tile);
        }
        
        navMesh.BuildNavMesh();
    }

    public void DestroyMap()
    {
        Action<int> destroyChild = (i) => { Destroy(grid.transform.GetChild(i).gameObject); };
        if (Application.isEditor)
            destroyChild = (i) => { DestroyImmediate(grid.transform.GetChild(i).gameObject); };
            
        for (int i = grid.transform.childCount-1; i >= 0; i--) {
            destroyChild(i);
        }
        tiles.Clear();
        navMesh.RemoveData();
    }

    void Update()
    {
        if (update)
        {
            update = false;
            DestroyMap();
            BuildMap();
            return;
        }

        if (autoUpdate)
        {
            timeSpent += Time.deltaTime;
            if (timeSpent > timer)
            {
                timeSpent = 0;
                DestroyMap();
                BuildMap();
            }
            return;
        }
    }

    void LevelWater(Tile tile)
    {
        tile.transform.localScale = new Vector3(100, 100 + 0.2f * perlinHeight * 100, 100);
    }

    public Vector2 GetMapSize()
    {
        return mapSize;
    }

    public List<Tile> GetTiles()
    {
        return tiles;
    }
} 