using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable] public class Faction
{
    public string name = "";
    public Color  color;
    public int    crops  = 10;
    public int    lumber = 10;
    public int    stone  = 10;
    public List<Building> buildings = new();
}

public class FactionManager : MonoBehaviour
{
    [SerializeField] private Faction[] factions = new Faction[4];
    [SerializeField] private TextMeshProUGUI cropsText;
    [SerializeField] private TextMeshProUGUI lumberText;
    [SerializeField] private TextMeshProUGUI stoneText;

    private void Start()
    {
        if (String.IsNullOrWhiteSpace(factions[0].name) &&
            String.IsNullOrWhiteSpace(factions[1].name))
        {
            Debug.LogError("Not enough factions to start game.");
        }
    }
    
    public ref Faction GetFaction(int idx)
    {
        if (idx < 0 || idx > factions.Length) return ref factions[0];
        return ref factions[idx];
    }

    private void Update()
    {
        cropsText .text = factions[0].crops .ToString();
        lumberText.text = factions[0].lumber.ToString();
        stoneText .text = factions[0].stone .ToString();
    }
}
