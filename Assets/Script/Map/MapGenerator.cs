using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Generation
{
    public class Node
    {
        // TODO : use an int id for nodes comparison
        public Vector3 Position = Vector3.zero;
        public List<Node> Neighbours;
    }

    public class Connection
    {
        public int Cost;
        public Node FromNode;
        public Node ToNode;
    }

    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject navMeshObject;
        private List<NavMeshSurface> navMeshes;
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
        [Range(0, 100)][SerializeField] private int resourceSpawnChance = 1;
        [Range(0, 100)][SerializeField] private int propSpawnChance = 1;

        public List<Tile> tiles { get; private set; } = new();
        public List<Tile> naturalResources { get; private set; } = new();
        private float timer = 0.1f;
        private float timeSpent = 0.0f;
        private MeshStorage meshStorage = null;

        [SerializeField]
        protected int SquareSize = 1;
        [SerializeField]
        protected float MaxHeight = 10f;

        // enable / disable debug Gizmos
        [SerializeField]
        protected bool DrawGrid = false;
        [SerializeField]
        protected bool DrawNodes = false;
        [SerializeField]
        protected bool DrawConnections = false;

        // Grid parameters
        protected Vector3 GridStartPos = Vector3.zero;
        protected int NbTilesH = 0;
        protected int NbTilesV = 0;

        // Nodes
        protected List<Node> NodeList = new List<Node>();
        protected Dictionary<Node, List<Connection>> ConnectionGraph = new Dictionary<Node, List<Connection>>();

        public Action OnGraphCreated;

        public Dictionary<Node, List<Connection>> GetConnectionGraph()
        {
            return ConnectionGraph;
        }

        public void Awake()
        {
        }

        public void Start()
        {
            navMeshes = new List<NavMeshSurface>(navMeshObject.GetComponents<NavMeshSurface>());
            DestroyMap();
            BuildMap();
            CreateGraph();
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
                    naturalResources.Add(tile);
                }
                else {
                    SetTileProp(tile);
                }

                Node node = CreateAndSetupNode(new Vector3(tile.transform.position.x, tile.GetTileHeight(), tile.transform.position.z));
                NodeList.Add(node);
                tile.SetNode(node);
            }
            FindObjectOfType<InfluenceManager>().SetNaturalResources(naturalResources);

            // Update all navigation meshes.
            navMeshes.ForEach(navMesh => navMesh.BuildNavMesh());

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
        }

        public void DestroyMap()
        {
            Action<int> destroyChild = (i) => { Destroy(grid.transform.GetChild(i).gameObject); };
            if (Application.isEditor)
                destroyChild = (i) => { DestroyImmediate(grid.transform.GetChild(i).gameObject); };

            for (int i = grid.transform.childCount - 1; i >= 0; i--) {
                destroyChild(i);
            }
            tiles.Clear();
            navMeshes.ForEach(navMesh => navMesh.RemoveData());
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
            tile.transform.localScale = new Vector3(100, 100 + thresholds.colorKeys[(int)TileType.Water + 1].time * perlinHeight * 100, 100);
        }

        public Vector2 GetMapSize()
        {
            return mapSize;
        }

        public List<Tile> GetTiles()
        {
            return tiles;
        }

        protected virtual Node CreateNode()
        {
            return new Node();
        }

        virtual protected Node CreateAndSetupNode(Vector3 pos)
        {
            RaycastHit hitInfo = new RaycastHit();

            // Always compute node Y pos from floor collision
            if (Physics.Raycast(pos + Vector3.up * MaxHeight, Vector3.down, out hitInfo, MaxHeight + 1, 1 << LayerMask.NameToLayer("Tile")))
            {
                pos.y = hitInfo.point.y;
            }

            Node node = CreateNode();
            node.Position = pos;

            return node;
        }

        virtual protected Connection CreateConnection(Node from, Node to)
        {
            Connection connection = new Connection();
            connection.FromNode = from;
            connection.ToNode = to;

            return connection;
        }

        // Compute possible connections between each nodes
        virtual protected void CreateGraph()
        {
            foreach (Node node in NodeList)
            {
                if (IsNodeValid(node))
                {
                    ConnectionGraph.Add(node, new List<Connection>());
                    node.Neighbours = GetNeighbours(node); // cache neighbours list
                    foreach (Node neighbour in node.Neighbours)
                    {
                        ConnectionGraph[node].Add(CreateConnection(node, neighbour));
                    }
                }
            }

            OnGraphCreated?.Invoke();
        }

        public bool IsPosValid(Vector3 pos)
        {
            if (pos.x > (-mapSize.x / 2) && pos.x < (mapSize.x / 2) && pos.z > (-mapSize.y / 2) && pos.z < (mapSize.y / 2))
                return true;
            return false;
        }

        // Converts world 3d pos to tile 2d pos
        public Vector2Int GetTileCoordFromPos(Vector3 pos)
        {
            Vector3 realPos = pos - GridStartPos;
            Vector2Int tileCoords = Vector2Int.zero;
            tileCoords.x = Mathf.FloorToInt(realPos.x / SquareSize);
            tileCoords.y = Mathf.FloorToInt(realPos.z / SquareSize);
            return tileCoords;
        }

        public Node GetNode(Vector3 pos)
        {
            return GetNode(GetTileCoordFromPos(pos));
        }

        public Node GetNode(Vector2Int pos)
        {
            return GetNode(pos.x, pos.y);
        }

        protected Node GetNode(int x, int y)
        {
            int index = y * NbTilesH + x;
            if (index >= NodeList.Count || index < 0)
                return null;

            return NodeList[index];
        }

        virtual protected bool IsNodeValid(Node node)
        {
            return node != null;
        }

        private void AddNode(List<Node> list, Node node)
        {
            if (IsNodeValid(node))
                list.Add(node);
        }

        virtual protected List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbors = new List<Node>();

            Vector3Int[] directions = {
            new Vector3Int(1, 0, 0),   // East
            new Vector3Int(1, 0, -1),   // Southeast
            new Vector3Int(-1, 0, -1),   // Southwest
            new Vector3Int(-1, 0, 0),   // West
            new Vector3Int(-1, 0, 1),   // Northwest
            new Vector3Int(1, 0, 1)    // Northeast
            };

            List<Node> nodes = new List<Node>();
            double sin = Math.Sin(45);
            // Loop through each direction and add neighboring cells
            foreach (Vector3Int dir in directions)
            {
                if (dir.x + dir.y + dir.z == 1)
                {
                    if (Physics.Raycast(node.Position, Quaternion.AngleAxis(-25, new Vector3(dir.z, 0, dir.x)) * dir,
                        out RaycastHit hit, float.MaxValue, -5, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.gameObject.TryGetComponent(out Tile tile))
                        {
                            neighbors.Add(tile.GetNode());
                        }
                    }
                }

                else
                {
                    if (Physics.Raycast(node.Position, Quaternion.AngleAxis(-25, Vector3.Cross(dir, Vector3.up)) * dir,
                        out RaycastHit hit, float.MaxValue, -5, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.gameObject.TryGetComponent(out Tile tile))
                        {
                            neighbors.Add(tile.GetNode());
                        }
                    }
                }
            }

            return neighbors;
        }

        #region Gizmos

        protected virtual void DrawGridGizmo()
        {
            float gridHeight = 0.01f;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < NbTilesV + 1; i++)
            {
                Vector3 startPos = new Vector3(-mapSize.x / 2f, gridHeight, -mapSize.y / 2f + i * SquareSize);
                Gizmos.DrawLine(startPos, startPos + Vector3.right * mapSize.y);

                for (int j = 0; j < NbTilesH + 1; j++)
                {
                    startPos = new Vector3(-mapSize.x / 2f + j * SquareSize, gridHeight, -mapSize.y / 2f);
                    Gizmos.DrawLine(startPos, startPos + Vector3.forward * mapSize.y);
                }
            }
        }
        protected virtual void DrawConnectionsGizmo()
        {
            foreach (Node crtNode in NodeList)
            {
                if (ConnectionGraph.ContainsKey(crtNode))
                {
                    foreach (Connection c in ConnectionGraph[crtNode])
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(c.FromNode.Position, c.ToNode.Position);
                    }
                }
            }
        }
        protected virtual void DrawNodesGizmo()
        {
            for (int i = 0; i < NodeList.Count; i++)
            {
                Node node = NodeList[i];
                Gizmos.color = Color.white;
                Gizmos.DrawCube(node.Position, Vector3.one * SquareSize * 0.5f);
            }
        }

        private void OnDrawGizmos()
        {
            if (DrawGrid)
            {
                DrawGridGizmo();
            }
            if (DrawNodes)
            {
                DrawNodesGizmo();
            }
            if (DrawConnections)
            {
                DrawConnectionsGizmo();
            }
        }
        #endregion
    }
}