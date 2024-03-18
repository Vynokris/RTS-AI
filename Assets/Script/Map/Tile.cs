using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private bool hasTree; //Does this tile has a decorative tree on it

    public float noiseHeight = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool GetHasTree()
    {
        return hasTree;
    }

    public void SetHasTree(bool value)
    {
        hasTree = value;
    }
}
