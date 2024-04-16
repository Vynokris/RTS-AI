using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopType
{
    Coordinator,
    Knight,
    Archer,
    Golem,
}

public class TroopStorage : MonoBehaviour
{
    [SerializeField] private GameObject coordinatorPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject golemPrefab;

    public GameObject GetTroopPrefab(TroopType type)
    {
        return type switch
        {
            TroopType.Coordinator => coordinatorPrefab,
            TroopType.Knight      => knightPrefab,
            TroopType.Archer      => archerPrefab,
            TroopType.Golem       => golemPrefab,
            _ => null
        };
    }
}
