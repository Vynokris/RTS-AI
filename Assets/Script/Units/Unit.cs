using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Camera Camera;
    public UnityEngine.AI.NavMeshAgent Agent;
    public Animator Animator;

    // Start is called before the first frame update
    public void Start()
    {
        Camera = Camera.main;
    }

    // Update is called once per frame
    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                Agent.SetDestination(hit.point);
            }
        }

        Animator.SetFloat("Speed", Agent.velocity.magnitude);
    }
}
