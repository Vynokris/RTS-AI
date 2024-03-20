using System;
using UnityEngine;

[Serializable]
public class Faction
{
    public string name = "";
    public Color  color;
    public int    crops  = 10;
    public int    lumber = 10;
    public int    stone  = 10;
}

public class FactionManager : MonoBehaviour
{
    [SerializeField] private Faction[] factions = new Faction[4];

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
}
