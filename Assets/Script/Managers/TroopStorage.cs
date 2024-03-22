using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopType
{
    Golem,
    Knight
}

public class TroopStorage : MonoBehaviour
{
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject golemPrefab;

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
