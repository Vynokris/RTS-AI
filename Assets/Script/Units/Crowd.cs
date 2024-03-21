using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowd
{
    private readonly HashSet<Unit> selectedUnits = new HashSet<Unit>();
    private float slowestTroopSpeed = float.MaxValue;

    public void ComputeSlowestUnitSpeed()
    {
        float slowestSoFar = float.MaxValue;

        foreach (var unit in selectedUnits)
        {
            if (unit.GetMaxSpeed() < slowestSoFar)
                slowestSoFar = unit.GetMaxSpeed();
        }

        slowestTroopSpeed = slowestSoFar;
    }

    public void SetCrowdSpeed(float speed)
    {
        foreach (var unit in selectedUnits)
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
        foreach (var unit in selectedUnits)
        {
            unit.SetDestination(destination);
        }
    }

    public void AddUnitToCrowd(Unit unit)
    {
        selectedUnits.Add(unit);
    }

    public void RemoveUnitFromCrowd(Unit unit)
    {
        selectedUnits.Remove(unit);
    }
}
