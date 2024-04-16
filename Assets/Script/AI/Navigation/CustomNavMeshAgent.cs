using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomNavMeshAgent : MonoBehaviour
{
    private AStar aStar;
    private List<Generation.Node> path;

    public delegate void OnPathReadyHandler();
    public event OnPathReadyHandler OnPathReady;

    public Vector3 velocity = new Vector3(0, 0, 0);
    public float speed = 0.0f;
    public float remainingDistance = 0.0f;
    public bool pathPending = false;
    public bool hasPath = false;

    public float timer = 0.0f; //TESTS
    public int iterator = 0; //

    public void Awake()
    {
        aStar = gameObject.AddComponent<AStar>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (hasPath)
        {
            timer += Time.deltaTime;

            if (timer > 1.0f)
            {
                timer = 0.0f;
                transform.position = path[iterator++].Position;

                if (iterator == path.Count - 1)
                {
                    hasPath = false;
                }
            }
        }
    }

    public void SetDestination(Vector3 destination)
    {
        path = aStar.FindPath(gameObject.transform.position, destination);
        hasPath = true;
    }

    public void ResetPath()
    {
        path.Clear();
        hasPath = false;
    }

    public void Warp(Vector3 position)
    {
        transform.position = position;
    }
}
