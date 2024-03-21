using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FactionManager : MonoBehaviour
{
    [SerializeField] private GameObject playerGameObject;
    [SerializeField] private GameObject aiGameObject;

    [SerializeField] private int playerCount = 2;

    [SerializeField] private List<Faction> factions = new(4);

    [SerializeField] private RectTransform selectionBoxTransform;
    [SerializeField] private TextMeshProUGUI cropsText;
    [SerializeField] private TextMeshProUGUI lumberText;
    [SerializeField] private TextMeshProUGUI stoneText;

    private void Start()
    {
        GameObject player = Instantiate(playerGameObject);
        player.GetComponent<Player>().SetSelectionBox(selectionBoxTransform);
        factions.Add(player.GetComponent<Faction>());

        for (int i = 0; i < playerCount - 1; i++)
        {
            factions.Add(Instantiate(aiGameObject).GetComponent<Faction>());
        }

        if (!CheckFaction(0) && !CheckFaction(1))
        {
            Debug.LogError("Not enough factions to start game.");
        }
    }

    public bool CheckFaction(int idx)
    {
        if (idx < 0 || idx > factions.Count) return false;
        return !string.IsNullOrWhiteSpace(factions[idx].name);
    }
    
    public Faction GetFaction(int idx)
    {
        if (idx < 0 || idx > factions.Count) return factions[0];
        return factions[idx];
    }

    public List<Faction> GetFactions()
    {
        return factions;
    }

    private void Update()
    {
        cropsText .text = factions[0].crops .ToString();
        lumberText.text = factions[0].lumber.ToString();
        stoneText .text = factions[0].stone .ToString();
    }
}
