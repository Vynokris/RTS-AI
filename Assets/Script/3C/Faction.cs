using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Faction : MonoBehaviour
{
    public string designation = "";
    public Color color;
    public int crops = 10;
    public int lumber = 10;
    public int stone = 10;
    public Tile spawnTile = null;
    public List<Tile> ownedTiles = new();

    protected Crowd crowd = new();
    protected List<Unit> units = new();

    public void Start()
    {
        Unit golem = Instantiate(TroopStorage.instance.GetTroopPrefab(TroopType.Golem)).GetComponent<Unit>();
        golem.SetUnitColor(color);

        Unit knight = Instantiate(TroopStorage.instance.GetTroopPrefab(TroopType.Knight)).GetComponent<Unit>();
        knight.SetUnitColor(color);

        units.Add(golem);
        units.Add(knight);
    }
}
