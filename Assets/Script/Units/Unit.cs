using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer selectionSprite;

    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Animator animator;

    [SerializeField] protected float maxSpeed;

    // Start is called before the first frame update
    public void Start()
    {
        maxSpeed = agent.speed;
    }

    // Update is called once per frame
    public void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    public void SetDestination(Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    public void Select()
    {
        selectionSprite.gameObject.SetActive(true);
    }

    public void DeSelect()
    {
        selectionSprite.gameObject.SetActive(false);
    }

    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    public float GetCurrentSpeed()
    {
        return agent.speed;
    }

    public void SetUnitSpeed(float speed)
    {
        agent.speed = speed;
    }
}
