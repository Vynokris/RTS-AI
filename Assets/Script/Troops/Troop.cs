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

    private float attackPathRefreshTimer = 1.0f;
    private float attackRefreshTimer = 0.0f;

    private void Awake()
    {
        stateMachine.CreateState("Idle");
        stateMachine.CreateState("Navigate");
        stateMachine.CreateState("Guard", GuardState);
        stateMachine.CreateState("Attack", AttackState);

        stateMachine.CreateConnection("Navigate", "Idle", HasReachedDestination);
        stateMachine.CreateConnection("Guard", "Idle", IsNoThreatNearby);
        stateMachine.CreateConnection("Attack", "Idle", IsNoMoreTargetInRange);
        stateMachine.CreateConnection("Idle", "Guard", IsGettingAttacked);
    }

    public void Start()
    {
        blackBoard.SetMaxSpeed(agent.speed);
        attackRefreshTimer = blackBoard.GetAttackDelay();
    }

    public void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
        animator.SetBool("Attacking", false);
        attackRefreshTimer -= Time.deltaTime; //Here so that the attack cooldown isn't on pause if the player isn't attacking
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

    public void Attack(Troop enemy)
    {
        enemy.TakeDamage(blackBoard.GetDamage());
    }

    public void TakeDamage(float damage)
    {
        blackBoard.SetLife(blackBoard.GetLife() - damage);
        Debug.Log(blackBoard.GetLife());

        if (blackBoard.GetLife() < 0)
        {
            Destroy(gameObject); //TODO: Remove this troop's references
        }
    }

    public NavMeshAgent GetAgent()
    {
        return agent;
    }

    public FiniteStateMachine GetStateMachine()
    {
        return stateMachine;
    }

    public BlackBoard GetBlackBoard()
    {
        return blackBoard;
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

    #region StateUpdate

    private void GuardState()
    {

    }

    private void AttackState()
    {
        List<Troop> nearingEnemies = blackBoard.GetNearingEnemies();

        if (nearingEnemies.Count > 0)
        {
            if (attackRefreshTimer <= 0.0f)
            {
                if (Vector3.Distance(transform.position, nearingEnemies[0].transform.position) < blackBoard.GetRange())
                {
                    animator.SetBool("Attacking", true);
                    Attack(nearingEnemies[0]);
                    attackRefreshTimer = blackBoard.GetAttackDelay();
                }
            }
        }

        else
        {
            attackPathRefreshTimer -= Time.deltaTime;

            if (attackPathRefreshTimer < 0.0f)
            {
                agent.SetDestination(blackBoard.GetTarget().transform.position);
                attackPathRefreshTimer = 1.0f;
            }
        }
    }

    #endregion

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

    private void OnTriggerEnter(Collider other)
    {
        Troop troop = other.gameObject.GetComponent<Troop>();

        if (troop.owningFaction == owningFaction)
            return;

        blackBoard.GetNearingEnemies().Add(troop);
    }

    private void OnTriggerExit(Collider other)
    {
        Troop troop = other.gameObject.GetComponent<Troop>();

        if (troop.owningFaction == owningFaction)
            return;

        blackBoard.GetNearingEnemies().Remove(troop);
    }
}
