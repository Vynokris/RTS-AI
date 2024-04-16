using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Coordinator : MonoBehaviour
{
    [SerializeField] private CustomNavMeshAgent agent;
    [SerializeField] private float pathUpdateDelay = 0.5f;

    private Formation formation = Formation.None;

    private Vector3 position;
    //private float radius;

    private HashSet<Troop> troops;
    private float timer;

    // Start is called before the first frame update

    public void Awake()
    {
        agent = gameObject.AddComponent<CustomNavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;

        if (agent.hasPath && timer <= 0)
        {
            if (formation == Formation.None)
            {
                foreach (var troop in troops)
                {
                    troop.SetDestination(transform.position);
                }
            }

            else
            {
                List<Vector3> positions = CalculatePositions();
                int positionIndex = 0;

                foreach (var troop in troops)
                {
                    troop.SetDestination(positions[positionIndex]);
                    positionIndex++;
                }

                timer = pathUpdateDelay;
            }
        }
    }

    private List<Vector3> CalculatePositions()
    {
        switch (formation)
        {
            case Formation.Square:
                return CalculatesSquarePositions();

            case Formation.Circle:
                return CalculateCirclePositions();

            case Formation.None:
                throw new ArgumentOutOfRangeException();

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private List<Vector3> CalculatesSquarePositions()
    {
        List<Vector3> positions = new List<Vector3>(troops.Count);
        var size = Math.Sqrt(troops.Count);
        size = Math.Ceiling(size);
        float middleSize = (float)size * 0.5f;
        var middleOffset = new Vector3(middleSize, 0, middleSize);

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var pos = new Vector3(x, 0, z);

                pos += transform.position - middleOffset;

                positions.Add(pos);
            }
        }

        return positions;
    }

    private List<Vector3> CalculateCirclePositions()
    {
        List<Vector3> positions = new List<Vector3>(troops.Count);

        for (int i = 0; i < troops.Count; i++)
        {
            var angle = i * Mathf.PI * 2 / troops.Count;
            var x = Mathf.Cos(angle);
            var z = Mathf.Sin(angle);

            var pos = new Vector3(x, 0, z);

            pos += transform.position;

            positions.Add(pos);
        }

        return positions;
    }

    public void ComputeCrowdSize()
    {
        if (troops.Count == 0) return;

        Vector3 sum = new Vector3();


        foreach (var troop in troops)
        {
            sum += troop.transform.position;
        }

        position = sum / troops.Count;

        //float farthestTroopDistance = 0.0f;

        //foreach (var troop in troops)
        //{
        //    float deltaX = troop.transform.position.x - position.x;
        //    float deltaZ = troop.transform.position.z - position.z;

        //    float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

        //    if (distance > farthestTroopDistance)
        //        farthestTroopDistance = distance;
        //}

        //radius = farthestTroopDistance;

        agent.Warp(position);
    }

    public CustomNavMeshAgent GetAgent()
    {
        return agent;
    }

    public void SetCrowdDestination(Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    public void SetFormation(Formation newFormation)
    {
        formation = newFormation;
    }

    public void SetTroopRef(ref HashSet<Troop> refTroops)
    {
        troops = refTroops;
    }
}

public enum Formation
{
    None,
    Square,
    Circle
}
