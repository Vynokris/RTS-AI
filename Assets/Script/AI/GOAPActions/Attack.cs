using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : GOAPAction
{
    public Attack(string n, WorldState pre, WorldState eff) : base(n, pre, eff)
    {
    }

    public override bool Execute(GOAP goap)
    {
        throw new System.NotImplementedException();
    }
}
