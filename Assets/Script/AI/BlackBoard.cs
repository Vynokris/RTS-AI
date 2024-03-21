using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackBoard : MonoBehaviour
{
    [SerializeField] protected float maxLife;

    protected float life;
    protected float maxSpeed;

    public float GetLife()
    {
        return life;
    }

    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    public void SetMaxSpeed(float speed)
    {
        maxSpeed = speed;
    }
}
