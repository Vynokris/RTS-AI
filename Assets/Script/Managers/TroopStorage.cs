using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopType
{
    Golem,
    Knight,
    Archer
}

public class TroopStorage : MonoBehaviour
{
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject golemPrefab;
    [SerializeField] private GameObject archerPrefab;

    public GameObject GetTroopPrefab(TroopType type)
    {
        return type switch
        {
            TroopType.Golem => golemPrefab,
            TroopType.Knight => knightPrefab,
            TroopType.Archer => archerPrefab,
            _ => null
        };
    }
}
