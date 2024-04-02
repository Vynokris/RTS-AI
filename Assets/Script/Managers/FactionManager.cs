using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FactionManager : MonoBehaviour
{
    [SerializeField] private GameObject playerGameObject;
    [SerializeField] private GameObject aiGameObject;

    [SerializeField] private int playerCount = 2;

    private Dictionary<uint, Faction> factions = new();
    public static Player playerFaction { get; private set; }

    private void Start()
    {
        Player player = Instantiate(playerGameObject).GetComponent<Player>();
        factions.Add(player.GetID(), player);
        playerFaction = player;

        for (int i = 1; i < playerCount; i++)
        {
            Faction ai = Instantiate(aiGameObject).GetComponent<Faction>();
            factions.Add(ai.GetID(), ai);
        }

        if (factions.Count < 2)
        {
            Debug.LogError("Not enough factions to start game.");
        }
    }
    
    public Faction TryGetFaction(uint id)
    {
        if (id is Faction.unassignedID || !factions.TryGetValue(id, out Faction faction)) return null;
        return faction;
    }

    public Dictionary<uint, Faction> GetFactions()
    {
        return factions;
    }
}
