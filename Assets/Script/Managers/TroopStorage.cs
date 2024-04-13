using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopType
{
    Coordinator,
    Knight,
    Archer,
    Cavalier,
    Golem,
}

public class TroopStorage : MonoBehaviour
{
    [SerializeField] private GameObject coordinatorPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject cavalierPrefab;
    [SerializeField] private GameObject golemPrefab;

    public GameObject GetTroopPrefab(TroopType type)
    {
        return type switch
        {
            TroopType.Coordinator => coordinatorPrefab,
            TroopType.Knight      => knightPrefab,
            TroopType.Archer      => archerPrefab,
            TroopType.Cavalier    => cavalierPrefab,
            TroopType.Golem       => golemPrefab,
            _ => null
        };
    }
}
