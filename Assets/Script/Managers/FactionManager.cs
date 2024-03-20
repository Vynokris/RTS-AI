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
    public Tile       spawnTile  = null;
    public List<Tile> ownedTiles = new();
}

public class FactionManager : MonoBehaviour
{
    [SerializeField] private Faction[] factions = new Faction[4];
    [SerializeField] private TextMeshProUGUI cropsText;
    [SerializeField] private TextMeshProUGUI lumberText;
    [SerializeField] private TextMeshProUGUI stoneText;

    private void Start()
    {
        if (!CheckFaction(0) && !CheckFaction(1))
        {
            Debug.LogError("Not enough factions to start game.");
        }
    }

    public bool CheckFaction(int idx)
    {
        if (idx < 0 || idx > factions.Length) return false;
        return !string.IsNullOrWhiteSpace(factions[idx].name);
    }
    
    public ref Faction GetFaction(int idx)
    {
        if (idx < 0 || idx > factions.Length) return ref factions[0];
        return ref factions[idx];
    }

    public Faction[] GetFactions()
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
