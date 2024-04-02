using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Troop : MonoBehaviour
{
    private static NavMeshTriangulation triangulation;

    [LabelOverride("Unit Type")] [SerializeField] protected TroopType serializedType;
    [SerializeField] protected SpriteRenderer selectionSprite;

    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Animator animator;

    [SerializeField] protected FiniteStateMachine stateMachine;
    [SerializeField] protected BlackBoard blackBoard;
    
    public Faction owningFaction { get; private set; } = null;
    public TroopType type { get; private set; } = TroopType.Knight;

    private void Awake()
    {
        stateMachine.CreateState("Idle");
        stateMachine.CreateState("Navigate");
        stateMachine.CreateState("Guard");
        stateMachine.CreateState("Attack");

        stateMachine.CreateConnection("Navigate", "Idle", HasReachedDestination);
        stateMachine.CreateConnection("Guard", "Idle", HasReachedDestination);
        stateMachine.CreateConnection("Attack", "Idle", HasReachedDestination);

        stateMachine.CreateConnection("Idle", "Guard", IsGettingAttacked);
    }

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

    public NavMeshAgent GetAgent()
    {
        return agent;
    }

    public FiniteStateMachine GetStateMachine()
    {
        return stateMachine;
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

    #region StateConnections

    private bool HasReachedDestination()
    {
        return agent.remainingDistance <= 0.1f && !agent.pathPending;
    }

    private bool IsNoThreatNearby()
    {
        return false; //TODO
    }

    private bool IsNoMoreTargetInRange()
    {
        return false; //TODO
    }

    private bool IsGettingAttacked()
    {
        return false; //TODO
    }

    #endregion
}
