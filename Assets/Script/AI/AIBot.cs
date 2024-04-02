using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIBot : Faction
{
    // All of the code inside is placeholder (for test purposes)

    private float timer;
    private NavMeshTriangulation triangulation;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        SelectAllTroops();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer <= 0)
        {
            timer = 7.0f;
            MoveCrowd();
        }

        timer -= Time.deltaTime;
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
}
