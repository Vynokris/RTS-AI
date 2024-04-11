using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldState
{
    public CustomBitArray state = new();

    //public bool Matches(WorldState other)
    //{
    //    for (int i = 0; i < state.Count; i++)
    //    {
    //        if (state[i] != other.state[i])
    //            return false;
    //    }
    //    return true;
    //}

    public WorldState Clone()
    {
        WorldState newState = new WorldState();
        newState.state = state;

        return newState;
    }
}
