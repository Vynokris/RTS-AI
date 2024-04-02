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

[Serializable] public struct PlayerNecessaryGameObjects
{
    public RectTransform selectionBox;
    public TextMeshProUGUI cropsText;
    public TextMeshProUGUI lumberText;
    public TextMeshProUGUI stoneText;
    public GameObject buildingUI;
    public Button castleSelectButton;
    public Button barracksSelectButton;
    public Button farmSelectButton;
    public Button lumbermillSelectButton;
    public Button mineSelectButton;
}

public class Player : Faction
{
    [SerializeField] private CursorParams defaultCursor;
    [SerializeField] private CursorParams buildCursor;
    [SerializeField] private CursorParams attackCursor;

    private PlayerNecessaryGameObjects objectRefs;
    private Plane   selectionDefaultPlane;
    private Vector3 selectionStartWorld;
    
    private CostStorage costStorage;
    private Camera cam;
    private bool inBuildMode = false;
    private BuildingType currentlyPlacingBuilding = BuildingType.None;
    
    public override void Start()
    {
        base.Start();
        costStorage = FindObjectOfType<CostStorage>();
        cam = Camera.main;
        selectionDefaultPlane = new Plane(Vector3.up, Vector3.zero);
        
        Ray ray = new Ray(spawnTile.transform.position + Vector3.up * (spawnTile.GetTileHeight() + 4.5f), cam.transform.rotation * Vector3.forward);
        if (selectionDefaultPlane.Raycast(ray, out float enter))
        {
            Vector3 camOrig = ray.origin + ray.direction * enter;
            Debug.Log(ray.origin.x + " " + ray.origin.y + " " + ray.origin.z + ", " + camOrig.x + " " + camOrig.y + " " + camOrig.z);
            cam.transform.parent.gameObject.GetComponent<CameraControls>().SetOrigin(camOrig);
        }

        Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
    }

    void Update()
    {
        UpdateCanvas();
        ModeManagement();

        if (!inBuildMode) {
            TroopManagement();
        }
        else {
            BuildingManagement();
        }
    }

    private void UpdateCanvas()
    {
        objectRefs.cropsText .text = crops .ToString("0.");
        objectRefs.lumberText.text = lumber.ToString("0.");
        objectRefs.stoneText .text = stone .ToString("0.");
    }

    private void ModeManagement()
    {
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            inBuildMode = !inBuildMode;
            if (inBuildMode) {
                Cursor.SetCursor(buildCursor.texture, buildCursor.hotspot, CursorMode.Auto);
                objectRefs.buildingUI.SetActive(true);
            }
            else {
                Cursor.SetCursor(defaultCursor.texture, defaultCursor.hotspot, CursorMode.Auto);
                objectRefs.buildingUI.SetActive(false);
            }
        }
    }

    private void TroopManagement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            objectRefs.selectionBox.sizeDelta = Vector2.zero;
            objectRefs.selectionBox.gameObject.SetActive(true);
            ScreenPointToMap(Input.mousePosition, out selectionStartWorld);
        }

        else if (Input.GetMouseButton(0))
        {
            ResizeSelectionBox();
        }

        else if (Input.GetMouseButtonUp(0))
        {
            objectRefs.selectionBox.sizeDelta = Vector2.zero;
            objectRefs.selectionBox.gameObject.SetActive(false);
            crowd.ComputeSlowestTroopSpeed();
            crowd.LimitCrowdSpeedToSlowest();
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                crowd.SetCrowdDestination(hit.point);
            }
        }
    }

    private void ResizeSelectionBox()
    {
        Vector2 selectionStartScreen = cam.WorldToScreenPoint(selectionStartWorld);
        float width  = Input.mousePosition.x - selectionStartScreen.x;
        float height = Input.mousePosition.y - selectionStartScreen.y;

        objectRefs.selectionBox.anchoredPosition = selectionStartScreen + new Vector2(width / 2, height / 2);
        objectRefs.selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        
        Bounds bounds = new Bounds(objectRefs.selectionBox.anchoredPosition, objectRefs.selectionBox.sizeDelta);
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
            
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("MapTile"))
                    && costStorage.GetBuilding(currentlyPlacingBuilding).TryPerform(ref crops, ref lumber, ref stone))
                {
                    Tile tile = hit.transform.gameObject.GetComponent<Tile>();
                    tile.SetBuilding(currentlyPlacingBuilding);
                    TakeOwnership(tile);
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

    public void SetNecessaryGameObjects(PlayerNecessaryGameObjects gameObjects)
    {
        objectRefs = gameObjects;
        
        objectRefs.castleSelectButton    .onClick.AddListener(() => currentlyPlacingBuilding = BuildingType.Castle);
        objectRefs.barracksSelectButton  .onClick.AddListener(() => currentlyPlacingBuilding = BuildingType.Barracks);
        objectRefs.farmSelectButton      .onClick.AddListener(() => currentlyPlacingBuilding = BuildingType.Farm);
        objectRefs.lumbermillSelectButton.onClick.AddListener(() => currentlyPlacingBuilding = BuildingType.Lumbermill);
        objectRefs.mineSelectButton      .onClick.AddListener(() => currentlyPlacingBuilding = BuildingType.Mine);
    }
}
