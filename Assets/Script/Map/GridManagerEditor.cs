using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))]
[CanEditMultipleObjects]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
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
        
        GUILayout.Space(15);
        base.OnInspectorGUI();
    }
}
