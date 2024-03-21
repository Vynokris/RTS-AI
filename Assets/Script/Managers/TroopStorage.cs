using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TroopStorage : MonoBehaviour
{
    public static TroopStorage instance { get; private set; }

    [SerializeField] private GameObject golemPrefab;
    [SerializeField] private GameObject knightPrefab;

    public void Awake()
    {
        instance = this;
    }

    public GameObject GetTroopPrefab(TroopType type)
    {
        return type switch
        {
            TroopType.Golem => golemPrefab,
            TroopType.Knight => knightPrefab,
            _ => null
        };
    }
}

public enum TroopType
{
    Golem,
    Knight
}

