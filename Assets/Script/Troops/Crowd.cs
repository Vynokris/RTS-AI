using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowd
{
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

    public void SetCrowdDestination(Vector3 destination)
    {
        foreach (var unit in troops)
        {
            unit.SetDestination(destination);
        }
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
}
