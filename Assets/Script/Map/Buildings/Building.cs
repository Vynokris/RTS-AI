using System;
using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] [LabelOverride("Max Health")] private float serializedMaxHealth = 100;
    protected Tile owningTile = null;
    public float maxHealth { get; private set; }
    public float health    { get; private set; }

    public void SetOwningTile(Tile tile)
    {
        owningTile = tile;
    }
    
    public void TakeDamage(float damage)
    {
        health = Mathf.Max(0, health - damage);
    }

    public void Repair(float amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    private void Start()
    {
        maxHealth = serializedMaxHealth;
    }
}
