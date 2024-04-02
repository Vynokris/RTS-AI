using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class UiManager : MonoBehaviour
{
    public RectTransform selectionBox;
    
    [Header("Resources")]
    public TextMeshProUGUI cropsText;
    public TextMeshProUGUI lumberText;
    public TextMeshProUGUI stoneText;
    
    [Header("Buildings")]
    public GameObject buildingUI;
    public Button castleSelectButton;
    public Button barracksSelectButton;
    public Button farmSelectButton;
    public Button lumbermillSelectButton;
    public Button mineSelectButton;
    
    [Header("Troop Training")]
    public GameObject troopTrainingUI;
    public Button knightSelectButton;
    public Button archerSelectButton;
    public Button cavalierSelectButton;
    public Button golemSelectButton;
    
    private Vector2 buildingUiDefaultPos;
    private Vector2 troopTrainingUiDefaultPos;

    private void Start()
    {
        buildingUiDefaultPos      = (buildingUI     .transform as RectTransform).anchoredPosition;
        troopTrainingUiDefaultPos = (troopTrainingUI.transform as RectTransform).anchoredPosition;
    }

    public void AddBuildingSelectButtonListener(BuildingType buildingType, UnityAction call)
    {
        Button button = buildingType switch
        {
            BuildingType.Farm       => farmSelectButton,
            BuildingType.Lumbermill => lumbermillSelectButton,
            BuildingType.Mine       => mineSelectButton,
            BuildingType.Barracks   => barracksSelectButton,
            BuildingType.Castle     => castleSelectButton,
            _ => null
        };
        if (button is not null) button.onClick.AddListener(call);
    }

    public void AddTroopSelectButtonListener(TroopType troopType, UnityAction call)
    {
        Button button = troopType switch
        {
            TroopType.Knight   => knightSelectButton,
            TroopType.Archer   => archerSelectButton,
            TroopType.Cavalier => cavalierSelectButton,
            TroopType.Golem    => golemSelectButton,
            _ => null
        };
        if (button is not null) button.onClick.AddListener(call);
    }

    public RectTransform GetSelectionBox()
    {
        return selectionBox;
    }

    public void UpdateResourcesText(float crops, float lumber, float stone)
    {
        cropsText .text = crops .ToString("0.");
        lumberText.text = lumber.ToString("0.");
        stoneText .text = stone .ToString("0.");
    }

    public void ToggleBuildingUI(bool visible)
    {
        if (buildingUI.activeSelf == visible) return;
        Vector2 onScreenPos  = buildingUiDefaultPos;
        Vector2 offScreenPos = onScreenPos + Vector2.down * 200;
        StartCoroutine(ToggleUI(buildingUI, visible, onScreenPos, offScreenPos, Vector2.down, .2f));
    }

    public void ToggleTroopTrainingUI(bool visible)
    {
        if (troopTrainingUI.activeSelf == visible) return;
        Vector2 onScreenPos  = troopTrainingUiDefaultPos;
        Vector2 offScreenPos = onScreenPos + Vector2.right * 200;
        StartCoroutine(ToggleUI(troopTrainingUI, visible, onScreenPos, offScreenPos, Vector2.right, .2f));
    }

    IEnumerator ToggleUI(GameObject ui, bool visible, Vector2 onScreenPos, Vector2 offScreenPos, Vector2 direction, float duration)
    {
        RectTransform uiTransform = ui.transform as RectTransform;
        if (!uiTransform) yield break;
        
        Vector2 start       = !visible ? onScreenPos : offScreenPos;
        Vector2 destination =  visible ? onScreenPos : offScreenPos;
        uiTransform.anchoredPosition = start;
        if (visible) ui.SetActive(true);
        
        float timer = 0;
        while (timer <= duration)
        {
            timer += Time.deltaTime;
            uiTransform.anchoredPosition = Vector2.Lerp(start, destination, timer / duration);
            yield return null;
        }

        if (!visible) ui.SetActive(false);
    }
}
