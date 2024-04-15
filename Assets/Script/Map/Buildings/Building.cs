using System;
using System.Collections;
using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] [LabelOverride("Max Health")] private float serializedMaxHealth = 100;
    [SerializeField] protected float repairDuration = 2;
    
    protected Tile owningTile = null;
    public float maxHealth { get; protected set; }
    public float health    { get; protected set; }
    public bool  repairing { get; protected set; } = false;

    public Tile GetOwningTile()
    {
        return owningTile;
    }

    public void SetOwningTile(Tile tile)
    {
        owningTile = tile;
    }
    
    public void TakeDamage(float damage)
    {
        health = Mathf.Max(0, health - damage);
    }

    public void Repair()
    {
        if (repairing) return;
        StartCoroutine(RepairOverTime());
    }
    
    private IEnumerator RepairOverTime()
    {
        repairing = true;
        float healthRepaired = 0;
        while (health < maxHealth && healthRepaired < maxHealth)
        {
            health += (maxHealth / repairDuration) * Time.deltaTime;
            yield return null;
        }
        repairing = false;
    }

    private void Start()
    {
        maxHealth = serializedMaxHealth;
    }
}
