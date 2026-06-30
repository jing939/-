using UnityEditor; 
using UnityEngine; 

public class SpriteImportSettings : AssetPostprocessor 
{ 
    void OnPreprocessTexture() 
    { 
        if (assetPath.Contains("Assets/Sprites/") || assetPath.Contains("Sprites/Nodes")) 
        { 
            TextureImporter ti = (TextureImporter)assetImporter; 
            ti.textureType = TextureImporterType.Sprite; 
        } 
    } 
}

