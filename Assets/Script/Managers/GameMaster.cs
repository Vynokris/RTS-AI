using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameMaster : MonoBehaviour
{
    [FormerlySerializedAs("gridManager")] [SerializeField] private MapGenerator mapGenerator;

    [SerializeField] private GameObject HQGameObject;
    [SerializeField] private int players = 2;
    [SerializeField] private List<Vector3> playerSpawns;

    private bool canSpawn = true;

    // Start is called before the first frame update
    void Start()
    {
        playerSpawns = new List<Vector3>(players);

        mapGenerator.DestroyMap();
        mapGenerator.BuildMap();

        SpawnPlayers();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < players; i++)
        {
            playerSpawns.Add(ChooseSpawnLocation());
            Instantiate(HQGameObject, playerSpawns[i], Quaternion.identity);
        }
    }

    Vector3 ChooseSpawnLocation()
    {
        var minDistance = mapGenerator.GetMapSize() / 4;

        List<Tile>tiles = mapGenerator.GetTiles();

        int randomIterator = Random.Range(0, tiles.Count);

        for (int i = randomIterator; i < tiles.Count; i++)
        {
            if (tiles[i].type == TileType.Grass && tiles[i].resourceType == ResourceType.None)
            {
                for (int j = 0; j < playerSpawns.Count; j++)
                {
                    float distanceX = tiles[i].transform.position.x - playerSpawns[j].x;
                    float distanceZ = tiles[i].transform.position.z - playerSpawns[j].z;

                    if (distanceX < minDistance.x && distanceZ < minDistance.y && playerSpawns.Count > 0)
                    {
                        canSpawn = false;
                        break;
                    }
                }

                if (canSpawn)
                {
                    return tiles[i].transform.position + new Vector3(0, tiles[i].GetTileHeight(), 0);
                }
            }

            canSpawn = true;
        }

        for (int i = 0; i < randomIterator; i++)
        {
            if (tiles[i].type == TileType.Grass &&  tiles[i].resourceType == ResourceType.None)
            {
                for (int j = 0; j < playerSpawns.Count; j++)
                {
                    float distanceX = tiles[i].transform.position.x - playerSpawns[j].x;
                    float distanceZ = tiles[i].transform.position.z - playerSpawns[j].z;

                    if (distanceX < minDistance.x && distanceZ < minDistance.y && playerSpawns.Count > 0)
                    {
                        canSpawn = false;
                        break;
                    }
                }

                if (canSpawn)
                {
                    return tiles[i].transform.position + new Vector3(0, tiles[i].GetTileHeight(), 0);
                }
            }

            canSpawn = true;
        }

        return Vector3.zero;
    }
}
