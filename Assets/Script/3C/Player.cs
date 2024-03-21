using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CursorParams
{
    public Texture2D texture;
    public Vector2 hotspot;
}

public class Player : Faction
{
    private Camera cam;
    
    [SerializeField] private CursorParams defaultCursor;
    [SerializeField] private CursorParams buildCursor;
    [SerializeField] private CursorParams attackCursor;

    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private LayerMask UnitLayer;
    private Vector2 startMousePos;

    new void Start()
    {
        base.Start();
        cam = Camera.main;

        Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
    }

    void Update()
    {
        HandleSelectionInput();
        HandleMoveInput();
    }

    private void HandleSelectionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("MapTile")))
            //{
            //    Tile tile = hit.transform.gameObject.GetComponent<Tile>();
            //    tile.SetBuilding(BuildingType.Barracks);
            //    tile.SetFaction(0);
            //    factionManager.GetFaction(0).ownedTiles.Add(tile);
            //}

            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(true);
            startMousePos = Input.mousePosition;
        }

        else if (Input.GetMouseButton(0))
        {
            ResizeSelectionBox();
        }

        else if (Input.GetMouseButtonUp(0))
        {
            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(false);
            crowd.ComputeSlowestUnitSpeed();
            crowd.LimitCrowdSpeedToSlowest();
        }
    }

    private void HandleMoveInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                crowd.SetCrowdDestination(hit.point);
            }
        }
    }

    private void ResizeSelectionBox()
    {
        float width = Input.mousePosition.x - startMousePos.x;
        float height = Input.mousePosition.y - startMousePos.y;

        selectionBox.anchoredPosition = startMousePos + new Vector2(width / 2, height / 2);
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

        Bounds bounds = new Bounds(selectionBox.anchoredPosition, selectionBox.sizeDelta);

        for (int i = 0; i < units.Count; i++)
        {
            if (UnitIsInSelectionBox(cam.WorldToScreenPoint(units[i].transform.position), bounds))
            {
                crowd.AddUnitToCrowd(units[i]);
                units[i].Select();
            }

            else
            {
                crowd.RemoveUnitFromCrowd(units[i]);
                units[i].DeSelect();
            }
        }
    }

    private bool UnitIsInSelectionBox(Vector3 position, Bounds bounds)
    {
        return position.x > bounds.min.x && position.x < bounds.max.x &&
               position.y > bounds.min.y && position.y < bounds.max.y;
    }

    public void SetSelectionBox(RectTransform selection)
    {
        selectionBox = selection;
    }
}
