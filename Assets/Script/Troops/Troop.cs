using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Troop : MonoBehaviour
{
    [LabelOverride("Unit Type")] [SerializeField] protected TroopType serializedType;
    [SerializeField] protected SpriteRenderer selectionSprite;

    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Animator animator;

    [SerializeField] protected FiniteStateMachine stateMachine;
    [SerializeField] protected BlackBoard blackBoard;
    
    public Faction owningFaction { get; private set; } = null;
    public TroopType type { get; private set; } = TroopType.Knight;

    public void Start()
    {
        blackBoard.SetMaxSpeed(agent.speed);
    }

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

    public void Deselect()
    {
        selectionSprite.gameObject.SetActive(false);
    }

    public float GetMaxSpeed()
    {
        return blackBoard.GetMaxSpeed();
    }

    public float GetCurrentSpeed()
    {
        return agent.speed;
    }

    public void SetUnitSpeed(float speed)
    {
        agent.speed = speed;
    }

    public void SetFaction(Faction faction)
    {
        owningFaction = faction;
        SetUnitColor(owningFaction.color);
    }

    public void SetUnitColor(Color color)
    {
        selectionSprite.color = color;
    }
}
