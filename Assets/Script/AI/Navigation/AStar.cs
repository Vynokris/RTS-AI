using Generation;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AStar : MonoBehaviour
{
    private MapGenerator mapGenerator;

    public void Awake()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
    }

    public List<Generation.Node> FindPath(Vector3 start, Vector3 end)
    {
        Generation.Node startNode = mapGenerator.GetNode(start);
        Generation.Node targetNode = mapGenerator.GetNode(end);

        List<Generation.Node> openList = new List<Generation.Node>();
        HashSet<Generation.Node> closedSet = new HashSet<Generation.Node>();

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Generation.Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost ||
                    openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost)
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (var connection in mapGenerator.GetConnectionGraph()[currentNode])
            {
                Generation.Node neighbour = connection.ToNode;
                if (neighbour == null || closedSet.Contains(neighbour))
                    continue;

                int newMovementCostToNeighbour = currentNode.GCost + connection.Cost;
                if (newMovementCostToNeighbour < neighbour.GCost || !openList.Contains(neighbour))
                {
                    neighbour.GCost = newMovementCostToNeighbour;
                    neighbour.HCost = CalculateDistance(neighbour, targetNode);
                    neighbour.Parent = currentNode;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        return null;
    }

    List<Generation.Node> RetracePath(Generation.Node startNode, Generation.Node endNode)
    {
        List<Generation.Node> path = new List<Generation.Node>();
        Generation.Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    int CalculateDistance(Generation.Node nodeA, Generation.Node nodeB)
    {
        try
        {
            return (int)(Mathf.Abs(nodeA.Position.x - nodeB.Position.x) + Mathf.Abs(nodeA.Position.y - nodeB.Position.y));
        }
        
        catch (Exception e)
        {
            return 0;
        }
    }
}