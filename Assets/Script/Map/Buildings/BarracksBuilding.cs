using UnityEngine;
using System.Collections.Generic;

public class BarracksBuilding : Building
{
    [SerializeField] private float secondsToTrainTroop = 5;
    private CostStorage costStorage;
    private readonly Queue<TroopType> troopTrainingQueue = new();
    private float troopTrainingTimer = 0;

    public void AddTroopToTrain(TroopType troopType)
    {
        Faction owningFaction = owningTile.owningFaction;
        ActionCost trainingCost = costStorage.GetTroopCost(troopType);
        if (trainingCost.TryPerform(ref owningFaction.crops, ref owningFaction.lumber, ref owningFaction.stone))
        {
            troopTrainingQueue.Enqueue(troopType);
            enabled = true;
        }
    }

    private void Start()
    {
        costStorage = FindObjectOfType<CostStorage>();
    }

    private void Update()
    {
        // Repetitively spawn troops according to the timer, and stop updating once the queue is empty.
        troopTrainingTimer += Time.deltaTime;
        if (troopTrainingTimer >= secondsToTrainTroop) 
        {
            troopTrainingTimer = 0;

            if (troopTrainingQueue.TryDequeue(out TroopType troopType))
            {
                owningTile.owningFaction.SpawnTroop(troopType, transform.position);

                if (troopTrainingQueue.Count <= 0) {
                    enabled = false;
                    troopTrainingTimer = 0;
                }
            }
        }
    }
}
