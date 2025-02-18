using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Troop : MonoBehaviour
{
    [SerializeField] [LabelOverride("Unit Type")] protected TroopType serializedType;
    [SerializeField] protected SpriteRenderer selectionSprite;

    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Animator animator;

    [SerializeField] protected FiniteStateMachine stateMachine;
    [SerializeField] protected BlackBoard blackBoard;

    [SerializeField] protected LayerMask sphereTriggerLayer;
    
    public Faction owningFaction { get; private set; } = null;
    public TroopType type { get; private set; } = TroopType.Knight;

    private float attackPathRefreshTimer = 1.0f;
    private float attackRefreshTimer = 0.0f;

    public bool underAttack { get; private set; } = false;

    private void Awake()
    {
        blackBoard.SetMaxSpeed(agent.speed);
        attackRefreshTimer = blackBoard.GetAttackDelay();

        stateMachine.CreateState("Idle");
        stateMachine.CreateState("Navigate");
        stateMachine.CreateState("Guard", GuardState);
        stateMachine.CreateState("Attack", AttackState);

        stateMachine.CreateConnection("Navigate", "Idle", HasReachedDestination);
        stateMachine.CreateConnection("Attack", "Idle", IsNoMoreTargetInRange);
        stateMachine.CreateConnection("Idle", "Guard", IsGettingAttacked);
    }

    public void Start()
    {

    }

    public void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
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

    public void Attack() // Called by an animation notifier
    {
        if (blackBoard.GetBuildingTarget())
        {
            blackBoard.GetBuildingTarget().TakeDamage(blackBoard.GetDamage());
            return;
        }

        if (blackBoard.GetTarget())
            blackBoard.GetTarget().TakeDamage(blackBoard.GetDamage());

        else
        {
            HashSet<Troop> enemies = blackBoard.GetNearingEnemies();

            foreach (var enemy in enemies)
            {
                if (Vector3.Distance(transform.position, enemy.transform.position) < blackBoard.GetRange())
                {
                    blackBoard.SetTarget(enemy);
                    break;
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        blackBoard.SetLife(blackBoard.GetLife() - damage);

        if (blackBoard.GetLife() <= 0)
        {
            owningFaction.DestroyTroop(this);
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

    private void AdvanceToEnemy(HashSet<Troop> nearingEnemies)
    {
        attackPathRefreshTimer -= Time.deltaTime;

        if (attackPathRefreshTimer <= 0.0f)
        {
            if (blackBoard.GetTarget())
            {
                agent.SetDestination(blackBoard.GetTarget().transform.position);
                attackPathRefreshTimer = 1.0f;
            }

            else if (nearingEnemies.Count > 0)
            {
                blackBoard.SetTarget(nearingEnemies.FirstOrDefault());
                agent.SetDestination(blackBoard.GetTarget().transform.position);
                attackPathRefreshTimer = 1.0f;
            }
        }
    }

    #region StateUpdate

    private void GuardState()
    {
        Building buildingTarget = blackBoard.GetBuildingTarget();

        if (buildingTarget)
        {
            agent.SetDestination(blackBoard.GetBuildingTarget().transform.position + new Vector3(Random.Range(-2, 3), 0, Random.Range(-2, 3)));
        }
        
        else
        {
            HashSet<Troop> nearingEnemies = blackBoard.GetNearingEnemies();

            if (nearingEnemies.Count > 0 && attackRefreshTimer <= 0.0f)
            {
                if (Vector3.Distance(transform.position, nearingEnemies.FirstOrDefault().transform.position) < blackBoard.GetRange())
                {
                    animator.Play("Attack");
                    blackBoard.SetTarget(nearingEnemies.FirstOrDefault());
                    attackRefreshTimer = blackBoard.GetAttackDelay();
                }
            }
        }
    }

    private void AttackState()
    {
        HashSet<Troop> nearingEnemies = blackBoard.GetNearingEnemies();

        if (nearingEnemies.Count > 0)
        {
            if (attackRefreshTimer <= 0.0f)
            {
                if (Vector3.Distance(transform.position, nearingEnemies.FirstOrDefault().transform.position) < blackBoard.GetRange())
                {
                    agent.ResetPath();
                    animator.Play("Attack");
                    blackBoard.SetTarget(nearingEnemies.FirstOrDefault());
                    attackRefreshTimer = blackBoard.GetAttackDelay();
                }

                else
                    AdvanceToEnemy(nearingEnemies);
            }
        }

        else if (!blackBoard.GetBuildingTarget())
        {
            AdvanceToEnemy(nearingEnemies);
        }

        else
        {
            if (attackRefreshTimer <= 0.0f)
            {
                if (Vector3.Distance(transform.position, blackBoard.GetBuildingTarget().transform.position) < blackBoard.GetRange())
                {
                    agent.ResetPath();
                    animator.Play("Attack");
                    attackRefreshTimer = blackBoard.GetAttackDelay();
                }

                else
                    agent.SetDestination(blackBoard.GetBuildingTarget().transform.position);
            }
        }
    }

    #endregion

    #region StateConnections

    private bool HasReachedDestination()
    {
        return agent.remainingDistance <= 0.4f && !agent.pathPending;
    }

    private bool IsNoThreatNearby()
    {
        return !IsGettingAttacked();
    }

    private bool IsNoMoreTargetInRange()
    {
        return !blackBoard.GetTarget() && blackBoard.GetNearingEnemies().Count == 0 && !blackBoard.GetBuildingTarget();
    }

    private bool IsGettingAttacked()
    {
        return owningFaction.GetCrowd().crowdUnderAttack;
    }

    #endregion

    private void OnTriggerEnter(Collider other)
    {
        Troop troop = other.gameObject.GetComponent<Troop>();
        if (!troop) return;

        if (sphereTriggerLayer == (sphereTriggerLayer | (1 << other.gameObject.layer)) || troop.owningFaction == owningFaction)
            return;

        blackBoard.GetNearingEnemies().Add(troop);

        if (stateMachine.GetCurrentStateName().Equals("Attack"))
            return;

        underAttack = true;
        owningFaction.GetCrowd().SetCrowdUnderAttack();
    }

    private void OnTriggerExit(Collider other)
    {
        Troop troop = other.gameObject.GetComponent<Troop>();
        
        if (!troop || troop.owningFaction == owningFaction || sphereTriggerLayer == (sphereTriggerLayer | (1 << other.gameObject.layer)))
            return;

        blackBoard.GetNearingEnemies().Remove(troop);

        if (stateMachine.GetCurrentStateName().Equals("Attack"))
            return;

        underAttack = false;
        owningFaction.GetCrowd().CheckIfCrowdUnderAttack();
    }
}
