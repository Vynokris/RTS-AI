using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class UtilitySystem : MonoBehaviour
{
    public List<UtilityAction> actions = new();
    [HideInInspector] public Type          functionCallerType;
    [HideInInspector] public MonoBehaviour functionCallerScript;

    public void PerformBestAction()
    {
        UtilityAction action = ChooseAction();
        Debug.Log(action.name);
        PerformAction(action);
    }
    
    public UtilityAction ChooseAction()
    {
        // Evaluate each available action.
        List<(UtilityAction, float)> evaluatedActions = EvaluateActions();
        float sum = 0; evaluatedActions.ForEach(element => sum += element.Item2);

        // Normalize the evaluated values into a probability distribution.
        for (int i = 0; i < evaluatedActions.Count; i++) 
        {
            evaluatedActions[i] = (evaluatedActions[i].Item1, evaluatedActions[i].Item2 / sum);
        }
        
        // Randomly choose an action using the probability distribution as weights.
        float rand = Random.Range(0f, 1f);
        sum = 0; UtilityAction selectedAction = actions[0];
        foreach (var element in evaluatedActions)
        {
            sum += element.Item2;
            if (rand < sum) {
                selectedAction = element.Item1;
                break;
            }
        }
        return selectedAction;
    }

    public List<(UtilityAction, float)> EvaluateActions()
    {
        List<(UtilityAction, float)> evaluatedActions = new();
        foreach (UtilityAction action in actions)
        {
            float score = EvaluateAction(action);
            evaluatedActions.Add((action,score));
        }
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
