using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BackgroundConfigurator
{
    static BackgroundConfigurator()
    {
        // 컴파일 완료 후 자동 실행되도록 대기열에 추가 (재정리 트리거)
        EditorApplication.delayCall += ConfigureBackgrounds;
    }

    [MenuItem("Custom/Configure Backgrounds")]
    public static void ConfigureBackgrounds()
    {
        Debug.Log("[BackgroundConfigurator] Starting background configuration...");

        // 1. 메인화면 및 탐색바탕 텍스처를 Sprite 포맷으로 변환
        SetTextureToSprite("Assets/base/메인화면.png");
        SetTextureToSprite("Assets/base/탐색바탕.png");

        // 2. 메인화면 이미지를 MainMenuBG.png로 복사/오버라이트
        CopyMainMenuBG();

        // 3. ExploreManager 프리팹에 탐색바탕 스프라이트 바인딩
        AssignExploreBackground();

        Debug.Log("[BackgroundConfigurator] Background configuration completed successfully!");
    }

    static void SetTextureToSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool modified = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }
            
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                modified = true;
            }

            if (modified)
            {
                importer.SaveAndReimport();
                Debug.Log($"[BackgroundConfigurator] Converted texture to Single Sprite: {path}");
            }
        }
        else
        {
            Debug.LogWarning($"[BackgroundConfigurator] TextureImporter not found for path: {path}");
        }
    }

    static void CopyMainMenuBG()
    {
        string srcPath = "Assets/base/메인화면.png";
        string destPath = "Assets/MainMenuBG.png";

        if (System.IO.File.Exists(srcPath))
        {
            System.IO.File.Copy(srcPath, destPath, true);
            AssetDatabase.ImportAsset(destPath);
            SetTextureToSprite(destPath);
            Debug.Log("[BackgroundConfigurator] Successfully copied 메인화면.png to MainMenuBG.png.");
        }
        else
        {
            Debug.LogError($"[BackgroundConfigurator] Source file not found: {srcPath}");
        }
    }

    static void AssignExploreBackground()
    {
        string prefabPath = "Assets/base/ExploreManager.prefab";
        string spritePath = "Assets/base/탐색바탕.png";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        if (prefab != null && bgSprite != null)
        {
            ExploreManager em = prefab.GetComponent<ExploreManager>();
            if (em != null)
            {
                if (em.mapBgSprite != bgSprite)
                {
                    em.mapBgSprite = bgSprite;
                    EditorUtility.SetDirty(prefab);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[BackgroundConfigurator] Assigned mapBgSprite on ExploreManager.prefab.");
                }
            }
            else
            {
                Debug.LogError("[BackgroundConfigurator] ExploreManager component not found on prefab.");
            }
        }
        else
        {
            Debug.LogError($"[BackgroundConfigurator] Prefab ({prefabPath}) or Sprite ({spritePath}) not found.");
        }
    }
}
