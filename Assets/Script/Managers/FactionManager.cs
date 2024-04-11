using System;
using System.Collections;
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
    
    protected InfluenceManager influenceManager;

    private void Start()
    {
        influenceManager = FindObjectOfType<InfluenceManager>();
        
        Player player = Instantiate(playerGameObject).GetComponent<Player>();
        player.AssignID(0);
        factions.Add(player.GetID(), player);
        playerFaction = player;

        for (uint i = 1; i < playerCount; i++)
        {
            Faction ai = Instantiate(aiGameObject).GetComponent<Faction>();
            ai.AssignID(i);
            factions.Add(ai.GetID(), ai);
        }

        if (factions.Count < 2)
        {
            Debug.LogError("Not enough factions to start game.");
        }
        
        StartCoroutine(UpdateTroopsInfluence());
    }

    private IEnumerator UpdateTroopsInfluence()
    {
        while (true)
        {
            List<List<Vector3>> troopPosPerFaction = new();
            foreach (var faction in factions)
            {
                List<Vector3> troopPos = new();
                foreach (Troop troop in faction.Value.troops) {
                    troopPos.Add(troop.transform.position);
                }
                troopPosPerFaction.Add(troopPos);
            }
            influenceManager.UpdateTroops(troopPosPerFaction);
            yield return new WaitForSeconds(1);
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
