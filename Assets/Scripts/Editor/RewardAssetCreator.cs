using UnityEngine;
using UnityEditor;

public class RewardAssetCreator
{
    [MenuItem("Tools/Create Reward RenderTexture")]
    public static void CreateRT()
    {
        string path = "Assets/RenderTextures/RT_RewardDisplay.renderTexture";
        RenderTexture rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        AssetDatabase.CreateAsset(rt, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Created RenderTexture at " + path);
    }
}
