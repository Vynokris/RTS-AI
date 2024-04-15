using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Generation.MapGenerator))]
[CanEditMultipleObjects]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Generation.MapGenerator mapGenerator = (Generation.MapGenerator)target;
        
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
