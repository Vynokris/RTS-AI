using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackBoard : MonoBehaviour
{
    [SerializeField] protected float maxLife;
    [SerializeField] protected float range;
    [SerializeField] private float damage;
    [SerializeField] private float attackDelay;

    private HashSet<Troop> nearingEnemies = new();

    private Troop target;

    protected float life;
    protected float maxSpeed;

    private void Start()
    {
        life = maxLife;
    }

    public float GetLife()
    {
        return life;
    }

    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    public float GetRange()
    {
        return range;
    }

    public float GetDamage()
    {
        return damage;
    }

    public float GetAttackDelay()
    {
        return attackDelay;
    }

    public HashSet<Troop> GetNearingEnemies()
    {
        nearingEnemies.RemoveWhere(item => item == null);
        return nearingEnemies;
    }

    public Troop GetTarget()
    {
        return target;
    }

    public void SetLife(float life)
    {
        this.life = life;
    }

    public void SetMaxSpeed(float speed)
    {
        maxSpeed = speed;
    }

    public void SetTarget(Troop troopTarget)
    {
        target = troopTarget;
    }
}
