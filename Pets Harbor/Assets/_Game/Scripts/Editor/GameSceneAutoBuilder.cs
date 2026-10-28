#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PetsHarbor.SlideMerge.Editor
{
    public static class GameSceneAutoBuilder
    {
        [MenuItem("PetsHarbor/Build Slide Merge Scene")]
        public static void BuildCompleteScene()
        {
            GameFeelTunerWindow.GetOrCreateConfigAsset();

            // Ensure texture import settings for Resources/UI textures
            ConfigureUITextureImport("Assets/_Game/Resources/UI/TopHeaderBanner.png");
            ConfigureUITextureImport("Assets/_Game/Resources/UI/EndcardScreen.png");

            ProceduralEmojiDrawer.ClearCaches();

            // 1. Configure Main Camera for 9:16 Portrait framing (Board in lower 72%, Header in top 28%)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                mainCam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            mainCam.transform.position = new Vector3(0f, 15.8f, -1.05f);
            mainCam.transform.rotation = Quaternion.Euler(78f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.66f, 0.42f, 0.24f, 1f);
            mainCam.fieldOfView = 46.5f;
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 50f;

            // 2. Configure Warm Directional Light for Glossy Jelly Highlights
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light dirLight = null;
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    dirLight = l;
                    break;
                }
            }
            if (dirLight == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.transform.position = new Vector3(0f, 10f, 0f);
            dirLight.transform.rotation = Quaternion.Euler(58f, -24f, 0f);
            dirLight.color = new Color(1.0f, 0.97f, 0.92f, 1f);
            dirLight.intensity = 1.35f;
            dirLight.shadows = LightShadows.Soft;

            // 3. Create or Rebuild GameManager root
            GameManager existingGm = Object.FindFirstObjectByType<GameManager>();
            if (existingGm != null)
            {
                Object.DestroyImmediate(existingGm.gameObject);
            }

            GameObject gmGo = new GameObject("GameManager_SlideMerge");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.SetupAndStartGame();

            // 4. Save Active Scene & also save a copy to Assets/_Game/Scenes/MainScene.unity
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            string scenesDir = "Assets/_Game/Scenes";
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{scenesDir}/MainScene.unity", true);
            AssetDatabase.Refresh();

            Debug.Log("[PetsHarbor] Built Slide Merge Scene successfully with 27 initial 3D Jelly Blocks (Total value = 2048 = Tier 12 Goal)!");
        }

        private static void ConfigureUITextureImport(string assetPath)
        {
            if (!File.Exists(assetPath)) return;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
