using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMesh;
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject tilePrefab;

    [Header("Map Generation")] 
    [SerializeField] private bool autoUpdate = false;
    [SerializeField] private bool update = false;
    [SerializeField] private Vector2 mapSize;
    [SerializeField] private Vector2 offset;
    [SerializeField] private int seed = 0;
    [SerializeField] private int octaves = 3;
    [SerializeField] private float perlinZoom = 14.0f;
    [SerializeField] private float perlinHeight = 2.0f;
    [SerializeField] private Gradient thresholds;
    [Range(0, 100)] [SerializeField] private int resourceSpawnChance = 1;
    [Range(0, 100)] [SerializeField] private int propSpawnChance = 1;
    
    private List<Tile> tiles = new();
    private float timer = 0.1f;
    private float timeSpent = 0.0f;
    private MeshStorage meshStorage = null;

    public void Start()
    {
        DestroyMap();
        BuildMap();
    }

    public void BuildMap()
    {
        if (meshStorage is null) {
            meshStorage = FindObjectOfType<MeshStorage>();
        }
        System.Random prng = new System.Random(seed);
        
        // Initialize perlin noise octaves.
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Generate map elevation from perlin noise octaves and create tiles.
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        for (int i = 0; i < mapSize.y; i++)
        {
            for (int j = 0; j < mapSize.x; j++)
            {
                var worldPos = grid.GetCellCenterWorld(new Vector3Int(j, i));
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.AngleAxis(60 * Random.Range(1, 3), Vector3.up), transform);

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
                tile.SetMeshStorage(meshStorage);
                tiles.Add(tile);
            }
        }

        // Set tile height and create props/resources.
        List<Vector3> naturalResourcePositions = new();
        foreach (var tile in tiles)
        {
            float finalSample = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, tile.noiseHeight);
            float scaleFactor = finalSample * perlinHeight * 100;
            tile.transform.localScale += new Vector3(0, scaleFactor, 0);
            Color color = thresholds.Evaluate(finalSample);
            TileType tileType = (TileType)Math.Clamp(Array.FindIndex(thresholds.colorKeys, element => element.color == color), 0, 100);
            
            if (tileType == TileType.Water)
                LevelWater(tile);

            tile.SetType(tileType);

            if (SetTileResource(tile)) {
                naturalResourcePositions.Add(tile.transform.position);
            }
            else {
                SetTileProp(tile);
            }
        }
        FindObjectOfType<InfluenceManager>().SetNaturalResources(naturalResourcePositions);
        
        // Set the spawn locations of all players.
        float minDistance = Vector3.Distance(tiles[0].transform.position, tiles[^1].transform.position) / 4;
        FactionManager factionManager = FindObjectOfType<FactionManager>();
        Dictionary<uint, Faction> factions = factionManager.GetFactions();
        foreach (uint i in factions.Keys)
        {
            Tile spawnTile = null;
            while (spawnTile is null) // There is a universe where this is an infinite loop...
            {
                Vector2 coords = new Vector2(Random.Range(0, (int)mapSize.x), Random.Range(0, (int)mapSize.y));
                Tile randTile = tiles[(int)(coords.y * mapSize.x + coords.x)];
                
                if (randTile.type is TileType.Grass &&
                    randTile.resourceType is ResourceType.None &&
                    randTile.propType is PropType.None or PropType.Breakable)
                {
                    bool isFarEnough = true;
                    foreach (uint j in factions.Keys)
                    {
                        if (j == i) break;
                        float distance = Vector2.Distance(randTile.transform.position, factions[j].spawnTile.transform.position);
                        if (distance < minDistance) isFarEnough = false;
                    }
                    if (isFarEnough) spawnTile = randTile;
                }
            }
            spawnTile.TrySetBuilding(BuildingType.Castle);
            factions[i].TakeOwnership(spawnTile, true);
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

    private bool SetTileProp(Tile tile)
    {
        List<Mesh> propMeshes = meshStorage.GetProps(tile.type);
        if (propMeshes is null || propMeshes.Count <= 0) return false;
        
        bool spawnProp = Random.Range(0, 100) < propSpawnChance;
        if (!spawnProp) return false;
        
        int random = Random.Range(0, propMeshes.Count);
        return tile.TrySetProp(propMeshes[random]);
    }

    private bool SetTileResource(Tile tile)
    {
        bool spawnResource = Random.Range(0, 100) < resourceSpawnChance;
        if (!spawnResource) return false;

        ResourceType resourceType = tile.type switch
        {
            TileType.Grass => Random.Range(0, 3) == 0 ? ResourceType.Stone : ResourceType.Lumber,
            TileType.Stone => ResourceType.Stone,
            _ => ResourceType.None,
        };
        if (resourceType is ResourceType.None) return false;

        return tile.TrySetResource(resourceType);
    }

    void LevelWater(Tile tile)
    {
        tile.transform.localScale = new Vector3(100, 100 + thresholds.colorKeys[(int)TileType.Water+1].time * perlinHeight * 100, 100);
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