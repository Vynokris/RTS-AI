using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RawTextureDataProcessing;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class InfluenceManager : MonoBehaviour
{
    [SerializeField] private ComputeShader influenceBlurShader;
    [SerializeField] private float influenceMapResolution = 100;
    private float worldMapAspectRatio = -1;
    private Vector2 textureSize = -Vector2.one;
    private float textureToWorld = -1;
    private float worldToTexture = -1;
    
    [SerializeField] private RawImage rawImageTest;
    private byte[] emptyPixelData;
    private Texture2D[] resourcesInfluence = new Texture2D[2]; // 1st: no blur, 2nd: blurred.
    private Texture2D[] buildingsInfluence = new Texture2D[2]; // 1st: no blur, 2nd: blurred.
    private Texture2D troopsInfluence;
    private RenderTexture blurRenderTexture;
    
    private MapGenerator mapGenerator;

    private void Awake()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        worldMapAspectRatio = mapGenerator.GetMapSize().x / mapGenerator.GetMapSize().y;
        textureSize = new Vector2(influenceMapResolution, influenceMapResolution * worldMapAspectRatio);
        worldToTexture = textureSize.x / mapGenerator.GetMapSize().x;
        textureToWorld = mapGenerator.GetMapSize().x / textureSize.x;
        
        emptyPixelData = Enumerable.Repeat((byte)0, (int)(textureSize.x * textureSize.y * 4)).ToArray();
        List<Texture2D> allTextures = new List<Texture2D> {
            resourcesInfluence[0], resourcesInfluence[1],
            buildingsInfluence[0], buildingsInfluence[1], troopsInfluence
        };
        for (int i = 0; i < allTextures.Count; i++)
        {
            allTextures[i] = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA32, false);
            allTextures[i].filterMode = FilterMode.Point;
            allTextures[i].SetPixelData(emptyPixelData, 0);
        }
        
        blurRenderTexture = new RenderTexture((int)textureSize.x, (int)textureSize.y, 0);
        blurRenderTexture.format = RenderTextureFormat.ARGB32;
        blurRenderTexture.enableRandomWrite = true;
        blurRenderTexture.Create();
        
        rawImageTest.texture = resourcesInfluence[1];
    }

    public Vector2 WorldToTexture(Vector3 worldCoords)
    {
        return new Vector2(worldCoords.x * worldToTexture, worldCoords.z * worldToTexture);
    }

    public Vector3 TextureToWorld(Vector2 texCoords)
    {
        return new Vector3(texCoords.x * textureToWorld, 0, texCoords.y * textureToWorld);
    }

    public int TextureToArray(Vector2 texCoords, int texWidth)
    {
        return (Mathf.RoundToInt(texCoords.y) * texWidth + Mathf.RoundToInt(texCoords.x)) * 4;
    }

    public Vector2 ArrayToTexture(int arrayIdx, int texWidth)
    {
        return new Vector2(Mathf.RoundToInt((float)arrayIdx / texWidth), arrayIdx % texWidth);
    }

    private void BlurTexture(Texture2D srcTex, Texture2D dtsTex)
    {
        int kernelHandle = influenceBlurShader.FindKernel("CSMain");
        influenceBlurShader.SetTexture(kernelHandle, "Result", blurRenderTexture);
        influenceBlurShader.SetTexture(kernelHandle, "ImageInput", srcTex);
        influenceBlurShader.Dispatch(kernelHandle, srcTex.width / 8, srcTex.height / 8, 1);
        
        RenderTexture.active = blurRenderTexture;
        dtsTex.ReadPixels(new Rect(0, 0, srcTex.width, srcTex.height), 0, 0);
        dtsTex.Apply();
    }

    public void SetNaturalResources(List<Tuple<ResourceType, Vector3>> resources)
    {
        var timer = new Stopwatch();
        timer.Start();
        
        NativeArray<byte> pixelData = resourcesInfluence[0].GetPixelData<byte>(0);
        foreach (Tuple<ResourceType, Vector3> tuple in resources)
        {
            ResourceType resourceType = tuple.Item1;
            Vector3      resourcePos  = tuple.Item2;
            
            Vector2 texCoords = WorldToTexture(resourcePos);
            int arrayIdx = TextureToArray(texCoords, resourcesInfluence[0].width);
            
            pixelData[arrayIdx+(int)resourceType-1] = 255;
            pixelData[arrayIdx+3] = 255;
        }
        
        resourcesInfluence[0].Apply();
        BlurTexture(resourcesInfluence[0], resourcesInfluence[1]);
        
        timer.Stop();
        Debug.Log("Influence map update took: " + timer.Elapsed.TotalMilliseconds * 1000 + "us");
    }

    /// Do not use in a loop!
    public void ClaimResource(int factionID, Vector3 position)
    {
        NativeArray<byte> pixelData = resourcesInfluence[0].GetPixelData<byte>(0);
        Vector2 texCoords = WorldToTexture(position);
        int arrayIdx = TextureToArray(texCoords, resourcesInfluence[0].width);
        
        pixelData[arrayIdx + factionID] = 255;
        pixelData[arrayIdx + 3]         = 255;
    }

    /// Do not use in a loop!
    public void AddBuilding(int factionID, Vector3 position)
    {
        NativeArray<byte> pixelData = buildingsInfluence[0].GetPixelData<byte>(0);
        Vector2 texCoords = WorldToTexture(position);
        int arrayIdx = TextureToArray(texCoords, buildingsInfluence[0].width);
        
        pixelData[arrayIdx + factionID] = 255;
        pixelData[arrayIdx + 3]         = 255;
        buildingsInfluence[0].Apply();
        BlurTexture(buildingsInfluence[0], buildingsInfluence[1]);
    }

    /// Do not use in a loop!
    public void RemoveBuilding(Vector3 position)
    {
        NativeArray<byte> pixelData = buildingsInfluence[0].GetPixelData<byte>(0);
        Vector2 texCoords = WorldToTexture(position);
        int arrayIdx = TextureToArray(texCoords, buildingsInfluence[0].width);
        
        for (int i = 0; i < 4; i++)
            pixelData[arrayIdx + i] = 0;
        buildingsInfluence[0].Apply();
        BlurTexture(buildingsInfluence[0], buildingsInfluence[1]);
    }
    
    /// Do not use every frame!
    public void UpdateTroops(List<Tuple<int, Vector3>> positions)
    {
        troopsInfluence.SetPixelData(emptyPixelData, 0);
        NativeArray<byte> pixelData = troopsInfluence.GetPixelData<byte>(0);

        foreach (Tuple<int, Vector3> tuple in positions)
        {
            int     factionID = tuple.Item1;
            Vector3 troopPos  = tuple.Item2;
            
            Vector2 texCoords = WorldToTexture(troopPos);
            int arrayIdx = TextureToArray(texCoords, troopsInfluence.width);
            
            pixelData[arrayIdx+factionID] = 255;
            pixelData[arrayIdx+3] = 255;
        }
        
        troopsInfluence.Apply();
        BlurTexture(troopsInfluence, troopsInfluence);
    }
}
