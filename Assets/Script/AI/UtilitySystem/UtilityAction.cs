using UnityEngine;

[CreateAssetMenu(fileName = "UtilityAction", menuName = "UtilitySystem/Action", order = 1)]
public class UtilityAction : ScriptableObject
{
    public float weight;
    public AnimationCurve curve;
    public string evalFuncName;
    public string performFuncName;
}
