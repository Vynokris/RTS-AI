using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Crowd
{
    private GameObject coordinatorPrefab;

    private Vector3 position;
    private float radius;

    public bool crowdUnderAttack { get; private set; }= false;

    private readonly HashSet<Troop> troops = new();
    private float slowestTroopSpeed = float.MaxValue;

    public void ComputeSlowestTroopSpeed()
    {
        float slowestSoFar = float.MaxValue;

        foreach (var unit in troops)
        {
            if (unit.GetMaxSpeed() < slowestSoFar)
                slowestSoFar = unit.GetMaxSpeed();
        }

        slowestTroopSpeed = slowestSoFar;
    }

    public void SetCrowdSpeed(float speed)
    {
        foreach (var unit in troops)
        {
            unit.SetUnitSpeed(speed);
        }
    }

    public void LimitCrowdSpeedToSlowest()
    {
        SetCrowdSpeed(slowestTroopSpeed);
    }

    public void SetCrowdTarget(Troop target)
    {
        foreach (var unit in troops)
        {
            unit.GetBlackBoard().SetTarget(target);
        }
    }

    public void SetCrowdDestination(Vector3 destination)
    {
        //foreach (var unit in troops)
        //{
        //    unit.SetDestination(destination);
        //}

        coordinatorPrefab.GetComponent<NavMeshAgent>().SetDestination(destination);
    }

    public void AddTroop(Troop troop)
    {
        troops.Add(troop);
        troop.Select();
    }

    public void RemoveTroop(Troop troop)
    {
        troops.Remove(troop);
        troop.Deselect();
    }

    public void RemoveAllTroops()
    {
        foreach (Troop troop in troops) {
            troop.Deselect();
        }
        troops.Clear();
    }

    public void ForceState(string stateName)
    {
        foreach (Troop troop in troops)
        {
            troop.GetStateMachine().ForceState(stateName);
        }
    }

    public void ComputeCrowdSize()
    {
        float sumX = 0.0f, sumZ = 0.0f;
        float farthestTroopDistance = 0.0f;

        foreach (var troop in troops)
        {
            sumX += troop.transform.position.x;
            sumZ += troop.transform.position.z;
        }

        float middleX = sumX / troops.Count;
        float middleZ = sumZ / troops.Count;

        position.x = middleX;
        position.z = middleZ;

        foreach (var troop in troops)
        {
            float deltaX = troop.transform.position.x - position.x;
            float deltaZ = troop.transform.position.z - position.z;

            float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

            if (distance > farthestTroopDistance)
                farthestTroopDistance = distance;
        }

        radius = farthestTroopDistance;
    }

    public void SetCoordinator(GameObject coordinator)
    {
        coordinatorPrefab = coordinator;
    }

    public void SetCrowdUnderAttack()
    {
        crowdUnderAttack = true;
    }

    public void CheckIfCrowdUnderAttack()
    {
        foreach (var unit in troops)
        {
            if (unit.underAttack)
                return;
        }

        crowdUnderAttack = false;
    }
}
