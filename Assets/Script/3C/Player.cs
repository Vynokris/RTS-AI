using System;
using UnityEngine;

[Serializable]
public struct CursorParams
{
    public Texture2D texture;
    public Vector2 hotspot;
}

public class Player : MonoBehaviour
{
    private Camera cam;
    private FactionManager factionManager;
    
    [SerializeField] private CursorParams defaultCursor;
    [SerializeField] private CursorParams buildCursor;
    [SerializeField] private CursorParams attackCursor;
    
    void Start()
    {
        cam = Camera.main;
        factionManager = FindObjectOfType<FactionManager>();
        Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("MapTile")))
            {
                Tile tile = hit.transform.gameObject.GetComponent<Tile>();
                tile.SetBuilding(BuildingType.Barracks);
                tile.SetFaction(0);
                factionManager.GetFaction(0).ownedTiles.Add(tile);
            }
        }
    }
}
