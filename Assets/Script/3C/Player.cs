using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable] public struct CursorParams
{
    public Texture2D texture;
    public Vector2 hotspot;
}

public class Player : Faction
{
    [SerializeField] private CursorParams defaultCursor;
    [SerializeField] private CursorParams buildCursor;
    [SerializeField] private CursorParams attackCursor;

    private Plane   selectionDefaultPlane;
    private Vector3 selectionStartWorld;
    
    private UiManager   uiManager;
    private Camera      cam;
    
    private bool inBuildMode = false;
    private BuildingType currentlyPlacingBuilding = BuildingType.None;
    private BarracksBuilding selectedBarracks = null;
    
    public override void Awake()
    {
        base.Awake();
        uiManager   = FindObjectOfType<UiManager>();
        cam         = Camera.main;
        selectionDefaultPlane = new Plane(Vector3.up, Vector3.zero);
        
        // Set the UI buttons callbacks.
        uiManager.AddBuildingSelectButtonListener(BuildingType.Castle,     () => currentlyPlacingBuilding = BuildingType.Castle);
        uiManager.AddBuildingSelectButtonListener(BuildingType.Barracks,   () => currentlyPlacingBuilding = BuildingType.Barracks);
        uiManager.AddBuildingSelectButtonListener(BuildingType.Farm,       () => currentlyPlacingBuilding = BuildingType.Farm);
        uiManager.AddBuildingSelectButtonListener(BuildingType.Lumbermill, () => currentlyPlacingBuilding = BuildingType.Lumbermill);
        uiManager.AddBuildingSelectButtonListener(BuildingType.Mine,       () => currentlyPlacingBuilding = BuildingType.Mine);
        uiManager.AddTroopSelectButtonListener(TroopType.Knight, () => selectedBarracks?.AddTroopToTrain(TroopType.Knight));
        uiManager.AddTroopSelectButtonListener(TroopType.Archer, () => selectedBarracks?.AddTroopToTrain(TroopType.Archer));
        uiManager.AddTroopSelectButtonListener(TroopType.Golem,  () => selectedBarracks?.AddTroopToTrain(TroopType.Golem));
        
        // Move the camera to the faction's spawn tile.
        spawnTileAssigned.AddListener(() =>
        {
            Ray ray = new Ray(spawnTile.transform.position + Vector3.up * (spawnTile.GetTileHeight() + 4.5f), cam.transform.rotation * Vector3.forward);
            if (selectionDefaultPlane.Raycast(ray, out float enter))
            {
                Vector3 camOrig = ray.origin + ray.direction * enter;
                Debug.Log(ray.origin.x + " " + ray.origin.y + " " + ray.origin.z + ", " + camOrig.x + " " + camOrig.y + " " + camOrig.z);
                cam.transform.parent.gameObject.GetComponent<CameraControls>().SetOrigin(camOrig);
                cam.transform.parent.position = camOrig;
            }
        });

        Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
    }

    void Update()
    {
        uiManager.UpdateResourcesText(crops, lumber, stone);
        ModeManagement();

        if (!inBuildMode) {
            TroopManagement();
        }
        else {
            BuildingManagement();
        }
    }

    private void ModeManagement()
    {
        // Switch between troop management and building modes.
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            inBuildMode = !inBuildMode;
            if (inBuildMode) {
                Cursor.SetCursor(buildCursor.texture, buildCursor.hotspot, CursorMode.Auto);
                uiManager.ToggleBuildingUI(true);
                uiManager.ToggleTroopTrainingUI(false);
            }
            else {
                Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
                uiManager.ToggleBuildingUI(false);
            }
        }
        
        // Open troop training window if in troop management mode and barracks building selected.
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0) && !inBuildMode)
        {
            Tile selectedTile = null;
            selectedBarracks  = null;
            
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("MapTile")))
            {
                selectedTile = hit.transform.gameObject.GetComponent<Tile>();
                if (selectedTile.buildingType is not BuildingType.Barracks) {
                    selectedTile = null;
                }
                else {
                    selectedBarracks = selectedTile.building as BarracksBuilding;
                }
            }
            uiManager.ToggleTroopTrainingUI(selectedTile is not null);
        }
    }

    private void TroopManagement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RectTransform selectionBox = uiManager.GetSelectionBox();
            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(true);
            ScreenPointToMap(Input.mousePosition, out selectionStartWorld);
        }

        else if (Input.GetMouseButton(0))
        {
            ResizeSelectionBox();
        }

        else if (Input.GetMouseButtonUp(0))
        {
            SelectTroopsInBox();
            RectTransform selectionBox = uiManager.GetSelectionBox();
            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(false);
            crowd.RepositionCoordinator();
            crowd.ComputeSlowestTroopSpeed();
            crowd.LimitCrowdSpeedToSlowest();
        }

        else if (Input.GetKeyDown(KeyCode.L)) // TODO: Implement this in a better way
        {
            crowd.SetFormation(Formation.Square);
        }

        else if (Input.GetKeyDown(KeyCode.P)) // TODO: Implement this in a better way
        {
            crowd.SetFormation(Formation.Circle);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, float.MaxValue, -5, QueryTriggerInteraction.Ignore))
            {
                hit.collider.gameObject.TryGetComponent(out Troop possibleTroop);
                hit.collider.gameObject.TryGetComponent(out Tile tile);
                Building possibleBuilding = !tile ? null : tile.GetBuilding();

                if (possibleTroop && possibleTroop?.owningFaction.id != id)
                {
                    crowd.ForceState("Attack");
                    crowd.SetCrowdTarget(possibleTroop);
                }

                else if (possibleBuilding)
                {
                    if (possibleBuilding.GetOwningTile().owningFaction.id != id)
                        crowd.ForceState("Attack");
                    else
                        crowd.ForceState("Guard");
                    
                    crowd.SetCrowdTarget(possibleBuilding);
                }

                else
                {
                    crowd.ForceState("Navigate");
                    crowd.SetCrowdDestination(hit.point);
                }
            }
        }
    }

    private void ResizeSelectionBox()
    {
        Vector2 selectionStartScreen = cam.WorldToScreenPoint(selectionStartWorld);
        float width  = Input.mousePosition.x - selectionStartScreen.x;
        float height = Input.mousePosition.y - selectionStartScreen.y;
        
        RectTransform selectionBox = uiManager.GetSelectionBox();
        selectionBox.anchoredPosition = selectionStartScreen + new Vector2(width / 2, height / 2);
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
    }
    
    private void SelectTroopsInBox()
    {
        RectTransform selectionBox = uiManager.GetSelectionBox();
        Bounds bounds = new Bounds(selectionBox.anchoredPosition, selectionBox.sizeDelta);
        foreach (var troop in troops)
        {
            Vector3 troopPosition = cam.WorldToScreenPoint(troop.transform.position);
            bool troopInSelection = troopPosition.x > bounds.min.x && troopPosition.x < bounds.max.x &&
                                    troopPosition.y > bounds.min.y && troopPosition.y < bounds.max.y;
            
            if (troopInSelection) crowd.AddTroop   (troop);
            else                  crowd.RemoveTroop(troop);
        }
    }

    private void BuildingManagement()
    {
        if (EventSystem.current.IsPointerOverGameObject()) 
        {
            Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
        }
        
        else 
        {
            Cursor.SetCursor(buildCursor.texture, buildCursor.hotspot, CursorMode.Auto);
            
            // Construct new buildings.
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("MapTile")))
                {
                    Tile tile = hit.transform.gameObject.GetComponent<Tile>();
                    CreateBuilding(tile, currentlyPlacingBuilding);
                }
            }

            // Destroy constructed buildings.
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("MapTile")))
                {
                    Tile tile = hit.transform.gameObject.GetComponent<Tile>();
                    if (tile != spawnTile)
                    {
                        DestroyBuilding(tile, true);
                    }
                }
            }
        }
        
    }

    private bool ScreenPointToMap(Vector2 screenPoint, out Vector3 worldPoint)
    {
        Ray ray = cam.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            worldPoint = hit.point;
            return true;
        }
        if (selectionDefaultPlane.Raycast(ray, out float enter)) {
            worldPoint = ray.origin + ray.direction * enter;
            return true;
        }
        worldPoint = Vector3.zero;
        return false;
    }
}
