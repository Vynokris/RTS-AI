using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilitySystem : MonoBehaviour
{
    public List<UtilityAction> actions = new();
    [HideInInspector] public Type          functionCallerType;
    [HideInInspector] public MonoBehaviour functionCallerScript;

    public void PerformBestAction()
    {
        PerformAction(ChooseAction());
    }
    
    public UtilityAction ChooseAction()
    {
        float bestScore = float.MinValue;
        UtilityAction bestAction = null;
        foreach (UtilityAction action in actions)
        {
            float score = EvaluateAction(action);
            if (score > bestScore)
            {
                bestScore  = score;
                bestAction = action;
            }
        }
        return bestAction;
    }

    public List<(UtilityAction, float)> EvaluateActions()
    {
        List<(UtilityAction, float)> evaluatedActions = new();
        foreach (UtilityAction action in actions)
        {
            float score = EvaluateAction(action);
            evaluatedActions.Add((action, score));
        }
        evaluatedActions.Sort((x, y) => x.Item2 < y.Item2 ? 1 : -1);
        return evaluatedActions;
    }

    public float EvaluateAction(UtilityAction action)
    {
        var methodInfo = functionCallerType.GetMethod(action.evalFuncName);
        if (methodInfo is null || methodInfo.ReturnType != typeof(float) || methodInfo.GetParameters().Length > 0) return 0;
        float value = (float)methodInfo.Invoke(functionCallerScript, new object[]{});
        return action.curve.Evaluate(Mathf.Clamp(value, 0, 1)) * action.weight;
    }
    
    public void PerformAction(UtilityAction action)
    {
        var methodInfo = functionCallerType.GetMethod(action.performFuncName);
        if (methodInfo is null) return;
        methodInfo.Invoke(functionCallerScript, new object[]{});
    }
}
