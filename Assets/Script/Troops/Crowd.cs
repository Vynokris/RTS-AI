using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Crowd
{
    private GameObject coordinatorPrefab;
    private Coordinator coordinator;

    public bool crowdUnderAttack { get; private set; }= false;

    private HashSet<Troop> troops = new();
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

        coordinator.GetAgent().SetDestination(destination);
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

    public void RepositionCoordinator()
    {
        coordinator.ComputeCrowdSize();
    }

    public void SetCoordinator(GameObject coordinator)
    {
        coordinatorPrefab = coordinator;
        this.coordinator = coordinatorPrefab.GetComponent<Coordinator>();
        this.coordinator.SetTroopRef(ref troops);
    }

    public void SetFormation(Formation formation)
    {
        coordinator.SetFormation(formation);
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
