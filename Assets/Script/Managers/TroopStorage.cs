using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopType
{
    Knight,
    Archer,
    Cavalier,
    Golem,
}

public class TroopStorage : MonoBehaviour
{
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject cavalierPrefab;
    [SerializeField] private GameObject golemPrefab;

    public GameObject GetTroopPrefab(TroopType type)
    {
        return type switch
        {
            TroopType.Knight   => knightPrefab,
            TroopType.Archer   => archerPrefab,
            TroopType.Cavalier => cavalierPrefab,
            TroopType.Golem    => golemPrefab,
            _ => null
        };
    }
}
