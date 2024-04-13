using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class AIBot : Faction
{
    [SerializeField] private GOAP goap;

    private Thread thread;
    private bool isStopped;

    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();
        thread = new Thread(Run);
    }

    void SelectAllTroops()
    {
        for (int i = 0; i < troops.Count; i++)
        {
            crowd.AddTroop(troops[i]);
        }

        crowd.ComputeSlowestTroopSpeed();
        crowd.LimitCrowdSpeedToSlowest();
    }

    void MoveCrowd()
    {
        crowd.ForceState("Navigate");
        Vector3 randomDirection = Random.insideUnitSphere * 10;
        NavMesh.SamplePosition(randomDirection, out var hit, 10.0f, 1);
        crowd.SetCrowdDestination(hit.position);
    }

    void Run()
    {
        while (!isStopped)
        {

        }
    }

    private void OnDestroy()
    {
        thread.Abort();
        isStopped = true;
    }
    private void OnApplicationQuit()
    {
        thread.Abort();
        isStopped = true;
    }
}
