using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject treePrefab;

    [Header("Meshes")]
    [SerializeField] private List<Mesh> stone;
    [SerializeField] private Mesh grass;
    [SerializeField] private List<Mesh> sand;
    [SerializeField] private Mesh water;

    [SerializeField] private Gradient thresholds;

    [Header("Map Generation")] 
    public bool autoUpdate = false;

    [SerializeField] private Vector2 mapSize;
    [SerializeField] private Vector2 offset;
    [SerializeField] private int seed = 0;
    [SerializeField] private int octaves = 3;
    [SerializeField] private float perlinZoom = 14.0f;
    [SerializeField] private float perlinHeight = 2.0f;
    [Range(0, 100)]
    [SerializeField] private int treeSpawnChance = 1;

    private List<Tile> tiles = new List<Tile>();

    private float timer = 0.1f;
    private float timeSpent = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        BuildMap();
    }

    void SpawnMeshFromColorGradient(MeshFilter meshFilter, Color color)
    {
        if (color.Equals(Color.blue))
        {
            meshFilter.mesh = water;
        }

        else if (color.Equals(new Color(1, 1, 0)))
        {
            int selectedMeshIndex = Random.Range(0, sand.Count);
            meshFilter.mesh = sand[selectedMeshIndex * Random.Range(0, 2) * Random.Range(0, 2)];
        }

        else if (color.Equals(Color.green))
        {
            meshFilter.mesh = grass;
            int treeSpawn = Random.Range(0, 101);

            if (treeSpawn <= treeSpawnChance)
            {
                Instantiate(treePrefab, meshFilter.gameObject.transform).transform.position += 
                    new Vector3(Random.Range(-0.2f, 0.2f), 0.2f, Random.Range(-0.2f, 0.2f));
                
                meshFilter.gameObject.GetComponent<Tile>().SetHasTree(true);
            }
        }

        else if (color.Equals(Color.black))
        {
            int selectedMeshIndex = Random.Range(0, stone.Count);
            meshFilter.mesh = stone[selectedMeshIndex * Random.Range(0, 2) * Random.Range(0, 2)];
        }
    }

    void BuildMap()
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
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

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

                Tile tile = tileObj.GetComponent<Tile>();
                tile.noiseHeight = totalSample;
                tiles.Add(tile);
            }
        }

        foreach (var tile in tiles)
        {
            float finalSample = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, tile.noiseHeight);

            tile.transform.position +=
                new Vector3(0, finalSample * perlinHeight, 0);
            var color = thresholds.Evaluate(finalSample);
            SpawnMeshFromColorGradient(tile.gameObject.GetComponent<MeshFilter>(), color);
        }
    }

    void DestroyMap()
    {
        for (int i = 0; i < grid.transform.childCount; i++)
        {
            Destroy(grid.transform.GetChild(i).gameObject);
        }

        tiles.Clear();
    }

    void Update()
    {
        if (autoUpdate)
        {
            timeSpent += Time.deltaTime;
            if (timeSpent > timer)
            {
                timeSpent = 0;
                DestroyMap();
                BuildMap();
            }
        }
    }
} 