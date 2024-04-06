using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InfluenceManager : MonoBehaviour
{
    [SerializeField] private float influenceMapResolution = 100;
    private float worldMapAspectRatio = -1;
    private Vector2 textureSize = -Vector2.one;
    private float textureToWorld = -1;
    private float worldToTexture = -1;
    
    [SerializeField] private RawImage rawImageTest;
    private Texture2D resourcesInfluence;
    private Texture2D buildingsInfluence;
    private Texture2D troopsInfluence;
    
    private MapGenerator mapGenerator;

    private void Awake()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        worldMapAspectRatio = mapGenerator.GetMapSize().x / mapGenerator.GetMapSize().y;
        textureSize = new Vector2(influenceMapResolution, influenceMapResolution * worldMapAspectRatio);
        worldToTexture = textureSize.x / mapGenerator.GetMapSize().x;
        textureToWorld = mapGenerator.GetMapSize().x / textureSize.x;
        
        resourcesInfluence = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA64, false);
        buildingsInfluence = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA64, false);
        troopsInfluence    = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA64, false);
        resourcesInfluence.filterMode = FilterMode.Point;
        buildingsInfluence.filterMode = FilterMode.Point;
        troopsInfluence   .filterMode = FilterMode.Point;
        
        short[] pixelData = Enumerable.Repeat((short)0, (int)(textureSize.x * textureSize.y * 4)).ToArray();
        resourcesInfluence.SetPixelData(pixelData, 0);
        buildingsInfluence.SetPixelData(pixelData, 0/*, (int)(textureSize.x * textureSize.y * 4) - (int)(textureSize.x * textureSize.y * 3)*/);
        troopsInfluence   .SetPixelData(pixelData, 0/*, (int)(textureSize.x * textureSize.y * 4) - (int)(textureSize.x * textureSize.y * 3)*/);
        
        rawImageTest.texture = resourcesInfluence;
    }

    public Vector2 WorldToTexture(Vector3 worldPos)
    {
        return new Vector2(worldPos.x * worldToTexture, worldPos.z * worldToTexture);
    }

    public Vector3 TextureToWorld(Vector2 texPos)
    {
        return new Vector3(texPos.x * textureToWorld, 0, texPos.y * textureToWorld);
    }

    public void SetNaturalResources(List<Vector3> positions)
    {
        NativeArray<ushort> pixelData = resourcesInfluence.GetPixelData<ushort>(0);

        foreach (Vector3 position in positions)
        {
            Vector2 texCoords = WorldToTexture(position);
            int arrayIdx = (int)(texCoords.y * textureSize.x + texCoords.x) * 4;
            pixelData[arrayIdx  ] = ushort.MaxValue;
            pixelData[arrayIdx+3] = ushort.MaxValue;
        }
        resourcesInfluence.Apply();
    }

    public void ClaimResource()
    {
        
    }

    /// Do not use in a loop!
    public void AddBuilding(int factionIdx, Vector3 position)
    {
        Vector2 texCoords = WorldToTexture(position);
        int arrayIdx = (Mathf.RoundToInt(texCoords.y) * buildingsInfluence.width + Mathf.RoundToInt(texCoords.x)) * 4;
        
        NativeArray<ushort> pixelData = buildingsInfluence.GetPixelData<ushort>(0);
        pixelData[arrayIdx + factionIdx] = ushort.MaxValue;
        pixelData[arrayIdx + 3]          = ushort.MaxValue;
        buildingsInfluence.Apply();
        
        Debug.Log("Faction " + factionIdx + " world coords (" + position.x + "|" + position.z + ") tex coords (" + texCoords.x + "|" + texCoords.y + ")");
    }
}
