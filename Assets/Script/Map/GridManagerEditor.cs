using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))]
[CanEditMultipleObjects]
public class GridManagerEditor : Editor
{
    
    void OnEnable()
    {
        Debug.Log("Hello there!");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GridManager gridManager = (GridManager)target;
        if (GUILayout.Button("Generate"))
        {
            gridManager.DestroyMap();
            gridManager.BuildMap();
        }

        if (GUILayout.Button("Destroy"))
        {
            gridManager.DestroyMap();
        }
    }
}
