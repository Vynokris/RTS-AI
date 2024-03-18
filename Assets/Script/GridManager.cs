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
    [SerializeField] private float perlinZoom = 14.0f;
    [SerializeField] private float perlinHeight = 2.0f;
    [Range(0, 100)]
    [SerializeField] private int treeSpawnChance = 1;

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
                Instantiate(treePrefab, meshFilter.gameObject.transform).transform.position += new Vector3(0, 0.2f, 0);
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
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 50; j++)
            {
                var worldPos = grid.GetCellCenterWorld(new Vector3Int(j, i));
                GameObject tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                float sample = Mathf.PerlinNoise(worldPos.x / perlinZoom, worldPos.z / perlinZoom);
                tile.transform.position += new Vector3(0, sample * perlinHeight , 0);
                var color = thresholds.Evaluate(sample);
                SpawnMeshFromColorGradient(tile.GetComponent<MeshFilter>(), color);
            }
        }
    }

    void DestroyMap()
    {
        for (int i = 0; i < grid.transform.childCount; i++)
        {
            Destroy(grid.transform.GetChild(i).gameObject);
        }
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