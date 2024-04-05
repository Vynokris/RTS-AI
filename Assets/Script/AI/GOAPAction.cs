using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class GOAPAction
{
    public string name;
    public WorldState preconditions;
    public WorldState effects;

    protected GOAPAction(string n, WorldState pre, WorldState eff)
    {
        name = n;
        preconditions = pre;
        effects = eff;
    }

    public bool CheckPreconditions(WorldState state)
    {
        return state.state.And(preconditions.state) == preconditions.state;
    }

    public virtual void Perform(ref WorldState state)
    {
        state.state = state.state.Or(effects.state);
    }

    public abstract bool Execute(GOAP goap);
}