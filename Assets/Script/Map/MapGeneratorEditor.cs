using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
[CanEditMultipleObjects]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator)target;
        
        if (GUILayout.Button("Generate"))
        {
            mapGenerator.DestroyMap();
            mapGenerator.BuildMap();
        }

        if (GUILayout.Button("Destroy"))
        {
            mapGenerator.DestroyMap();
        }
        
        GUILayout.Space(15);
        base.OnInspectorGUI();
    }
}
